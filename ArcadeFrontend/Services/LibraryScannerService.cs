using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public sealed class LibrarySourceDefinition
    {
        public string SourceId { get; init; } = string.Empty;
        public string RootPath { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;
        public string EmulatorProfileKey { get; init; } = string.Empty;
        public IReadOnlyList<string> SearchPatterns { get; init; } = Array.Empty<string>();
        public bool Recurse { get; init; } = true;
        public bool UseFilenameWithoutExtensionAsLaunchTarget { get; init; }
        public bool UseFullPathAsLaunchTarget { get; init; } = true;
        public bool IsEnabled { get; init; } = true;
    }

    public sealed class LibrarySourcesFile
    {
        public List<LibrarySourceDefinition> Sources { get; init; } = new();
    }

    public sealed class LibraryScanResult
    {
        public DateTime GeneratedUtc { get; init; } = DateTime.UtcNow;
        public int SourceCount { get; init; }
        public int GameCount { get; init; }
        public int AddedCount { get; init; }
        public int RemovedCount { get; init; }
        public string OutputPath { get; init; } = string.Empty;
        public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
        public IReadOnlyList<GameDefinition> Games { get; init; } = Array.Empty<GameDefinition>();
    }

    public interface ILibraryScannerService
    {
        string LastScanSummary { get; }
        OperationResult EnsureCatalog();
        OperationResult<LibraryScanResult> RescanCatalog();
        OperationResult<LibraryScanResult> EnsureCatalogAndLoad();
    }

    public sealed class LibraryScannerService : ILibraryScannerService
    {
        private readonly IPathService _pathService;
        private readonly ILoggingService _loggingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public string LastScanSummary { get; private set; } = "Library scan has not run yet.";

        public LibraryScannerService(IPathService pathService, ILoggingService loggingService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        public OperationResult EnsureCatalog()
        {
            var result = EnsureCatalogAndLoad();
            return result.IsSuccess
                ? OperationResult.Success(result.UserMessage)
                : OperationResult.Fail(result.UserMessage, result.FailureCategory, result.TechnicalMessage, result.Exception);
        }

        public OperationResult<LibraryScanResult> EnsureCatalogAndLoad()
        {
            try
            {
                EnsureLibrarySourcesFileExists();
                return RescanCatalog();
            }
            catch (Exception ex)
            {
                LastScanSummary = $"Library scan failed: {ex.Message}";
                _loggingService.Error(nameof(LibraryScannerService), "Failed to ensure library catalog.", ex, ex.Message);
                return OperationResult<LibraryScanResult>.Fail(
                    "The library catalog could not be prepared.",
                    FailureCategory.Unexpected,
                    ex.Message,
                    ex);
            }
        }

        public OperationResult<LibraryScanResult> RescanCatalog()
        {
            try
            {
                var sourcesPath = _pathService.GetLibrarySourcesPath();
                if (!File.Exists(sourcesPath))
                {
                    return OperationResult<LibraryScanResult>.Fail(
                        "The library source definition file could not be found.",
                        FailureCategory.MissingFile,
                        sourcesPath);
                }

                var sourcesJson = File.ReadAllText(sourcesPath);
                var sourcesFile = JsonSerializer.Deserialize<LibrarySourcesFile>(sourcesJson, _jsonOptions) ?? new LibrarySourcesFile();
                var enabledSources = sourcesFile.Sources.Where(s => s.IsEnabled).ToList();

                var previousCatalog = LoadExistingGeneratedCatalog();
                var previousIds = new HashSet<string>(previousCatalog.Select(g => g.Id), StringComparer.OrdinalIgnoreCase);

                var messages = new List<string>();
                var discoveredGames = new List<GameDefinition>();

                foreach (var source in enabledSources)
                {
                    if (string.IsNullOrWhiteSpace(source.RootPath))
                    {
                        messages.Add($"Skipped source '{source.SourceId}' because RootPath was blank.");
                        continue;
                    }

                    if (!Directory.Exists(source.RootPath))
                    {
                        messages.Add($"Skipped source '{source.SourceId}' because folder was not found: {source.RootPath}");
                        continue;
                    }

                    var patterns = source.SearchPatterns == null || source.SearchPatterns.Count == 0
                        ? new[] { "*.*" }
                        : source.SearchPatterns;

                    var searchOption = source.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var matchedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var pattern in patterns)
                    {
                        foreach (var file in Directory.EnumerateFiles(source.RootPath, pattern, searchOption))
                        {
                            matchedFiles.Add(file);
                        }
                    }

                    foreach (var file in matchedFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        var game = BuildGameDefinition(source, file);
                        if (discoveredGames.Any(g => string.Equals(g.Id, game.Id, StringComparison.OrdinalIgnoreCase)))
                        {
                            messages.Add($"Skipped duplicate generated Id '{game.Id}' from file '{file}'.");
                            continue;
                        }

                        discoveredGames.Add(game);
                    }

                    messages.Add($"Scanned source '{source.SourceId}' and found {matchedFiles.Count} file(s).");
                }

                var outputPath = _pathService.GetGeneratedGamesConfigPath();
                var output = new GameDataFile
                {
                    Games = discoveredGames
                };

                File.WriteAllText(outputPath, JsonSerializer.Serialize(output, _jsonOptions));

                var newIds = new HashSet<string>(discoveredGames.Select(g => g.Id), StringComparer.OrdinalIgnoreCase);
                var addedCount = newIds.Count(id => !previousIds.Contains(id));
                var removedCount = previousIds.Count(id => !newIds.Contains(id));

                var scanResult = new LibraryScanResult
                {
                    GeneratedUtc = DateTime.UtcNow,
                    SourceCount = enabledSources.Count,
                    GameCount = discoveredGames.Count,
                    AddedCount = addedCount,
                    RemovedCount = removedCount,
                    OutputPath = outputPath,
                    Messages = messages,
                    Games = discoveredGames
                };

                LastScanSummary =
                    $"Library scan complete. Sources: {scanResult.SourceCount} | Games: {scanResult.GameCount} | Added: {scanResult.AddedCount} | Removed: {scanResult.RemovedCount} | Output: {scanResult.OutputPath}";

                _loggingService.Info(nameof(LibraryScannerService), "Library scan complete.", LastScanSummary);

                return OperationResult<LibraryScanResult>.Success(
                    scanResult,
                    $"Library scan complete. {scanResult.GameCount} game(s) written.");
            }
            catch (Exception ex)
            {
                LastScanSummary = $"Library scan failed: {ex.Message}";
                _loggingService.Error(nameof(LibraryScannerService), "Library scan failed.", ex, ex.Message);
                return OperationResult<LibraryScanResult>.Fail(
                    "The library scan failed.",
                    FailureCategory.Unexpected,
                    ex.Message,
                    ex);
            }
        }

        private void EnsureLibrarySourcesFileExists()
        {
            var path = _pathService.GetLibrarySourcesPath();
            if (File.Exists(path))
            {
                return;
            }

            var defaults = new LibrarySourcesFile
            {
                Sources = new List<LibrarySourceDefinition>
                {
                    new()
                    {
                        SourceId = "mame",
                        RootPath = @"F:\Arcade\ROMs\Arcade\MAME",
                        Platform = "Arcade",
                        EmulatorProfileKey = "mame",
                        SearchPatterns = new[] { "*.zip" },
                        Recurse = false,
                        UseFilenameWithoutExtensionAsLaunchTarget = true,
                        UseFullPathAsLaunchTarget = false
                    },
                    new()
                    {
                        SourceId = "ps2",
                        RootPath = @"F:\Arcade\ROMs\Console\PS2",
                        Platform = "PS2",
                        EmulatorProfileKey = "pcsx2",
                        SearchPatterns = new[] { "*.iso", "*.bin" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "nes",
                        RootPath = @"F:\Arcade\ROMs\Console\NES",
                        Platform = "NES",
                        EmulatorProfileKey = "retroarch-nes",
                        SearchPatterns = new[] { "*.nes", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "snes",
                        RootPath = @"F:\Arcade\ROMs\Console\SNES",
                        Platform = "SNES",
                        EmulatorProfileKey = "retroarch-snes",
                        SearchPatterns = new[] { "*.smc", "*.sfc", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "genesis",
                        RootPath = @"F:\Arcade\ROMs\Console\Genesis",
                        Platform = "Genesis",
                        EmulatorProfileKey = "retroarch-genesis",
                        SearchPatterns = new[] { "*.gen", "*.md", "*.bin", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "gb",
                        RootPath = @"F:\Arcade\ROMs\Console\GameBoy",
                        Platform = "Game Boy",
                        EmulatorProfileKey = "retroarch-gb",
                        SearchPatterns = new[] { "*.gb", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "gbc",
                        RootPath = @"F:\Arcade\ROMs\Console\GameBoyColor",
                        Platform = "Game Boy Color",
                        EmulatorProfileKey = "retroarch-gbc",
                        SearchPatterns = new[] { "*.gbc", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "gamegear",
                        RootPath = @"F:\Arcade\ROMs\Console\GameGear",
                        Platform = "Game Gear",
                        EmulatorProfileKey = "retroarch-gamegear",
                        SearchPatterns = new[] { "*.gg", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "mastersystem",
                        RootPath = @"F:\Arcade\ROMs\Console\MasterSystem",
                        Platform = "Master System",
                        EmulatorProfileKey = "retroarch-mastersystem",
                        SearchPatterns = new[] { "*.sms", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "tg16",
                        RootPath = @"F:\Arcade\ROMs\Console\TurboGrafx16",
                        Platform = "TurboGrafx-16",
                        EmulatorProfileKey = "retroarch-tg16",
                        SearchPatterns = new[] { "*.pce", "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "neogeo",
                        RootPath = @"F:\Arcade\ROMs\Console\NeoGeo",
                        Platform = "Neo Geo",
                        EmulatorProfileKey = "retroarch-neogeo",
                        SearchPatterns = new[] { "*.zip" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    },
                    new()
                    {
                        SourceId = "ps1",
                        RootPath = @"F:\Arcade\ROMs\Console\PS1",
                        Platform = "PS1",
                        EmulatorProfileKey = "retroarch-ps1",
                        SearchPatterns = new[] { "*.cue", "*.chd", "*.pbp", "*.iso" },
                        Recurse = true,
                        UseFilenameWithoutExtensionAsLaunchTarget = false,
                        UseFullPathAsLaunchTarget = true
                    }
                }
            };

            File.WriteAllText(path, JsonSerializer.Serialize(defaults, _jsonOptions));
            _loggingService.Info(nameof(LibraryScannerService), "Created default library sources file.", path);
        }

        private List<GameDefinition> LoadExistingGeneratedCatalog()
        {
            var path = _pathService.GetGeneratedGamesConfigPath();
            if (!File.Exists(path))
            {
                return new List<GameDefinition>();
            }

            try
            {
                var json = File.ReadAllText(path);
                var existing = JsonSerializer.Deserialize<GameDataFile>(json, _jsonOptions) ?? new GameDataFile();
                return existing.Games ?? new List<GameDefinition>();
            }
            catch
            {
                return new List<GameDefinition>();
            }
        }

        private static GameDefinition BuildGameDefinition(LibrarySourceDefinition source, string filePath)
        {
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var relativePath = Path.GetRelativePath(source.RootPath, filePath);

            var launchTarget = source.UseFilenameWithoutExtensionAsLaunchTarget
                ? filenameWithoutExtension
                : filePath;

            return new GameDefinition
            {
                Id = BuildId(source.SourceId, relativePath),
                Title = BuildTitle(filenameWithoutExtension),
                Platform = source.Platform,
                LaunchTarget = launchTarget,
                EmulatorProfileKey = source.EmulatorProfileKey,
                Description = $"Discovered from library source '{source.SourceId}'."
            };
        }

        private static string BuildId(string sourceId, string relativePath)
        {
            var raw = $"{sourceId}-{relativePath.Replace(Path.DirectorySeparatorChar, '-').Replace(Path.AltDirectorySeparatorChar, '-')}";
            raw = Path.ChangeExtension(raw, null) ?? raw;
            var chars = raw
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray();

            var collapsed = new string(chars);
            while (collapsed.Contains("--", StringComparison.Ordinal))
            {
                collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
            }

            return collapsed.Trim('-');
        }

        private static string BuildTitle(string filenameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(filenameWithoutExtension))
            {
                return "Unknown Title";
            }

            return filenameWithoutExtension
                .Replace('_', ' ')
                .Replace('.', ' ')
                .Trim();
        }
    }
}