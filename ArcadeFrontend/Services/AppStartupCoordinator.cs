using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public sealed class AppStartupResult
    {
        public bool CanContinue { get; init; }
        public IReadOnlyList<string> StatusMessages { get; init; } = Array.Empty<string>();
        public StartupValidationReport? ValidationReport { get; init; }
        public IReadOnlyList<EmulatorProfile> EmulatorProfiles { get; init; } = Array.Empty<EmulatorProfile>();
        public GameDataLoadResult? GameData { get; init; }
    }

    public interface IAppStartupCoordinator
    {
        OperationResult<AppStartupResult> Initialize();
    }

    public sealed class AppStartupCoordinator : IAppStartupCoordinator
    {
        private readonly IPathService _pathService;
        private readonly ILoggingService _loggingService;
        private readonly IStartupValidationService _startupValidationService;
        private readonly IGameDataService _gameDataService;

        public AppStartupCoordinator(
            IPathService pathService,
            ILoggingService loggingService,
            IStartupValidationService startupValidationService,
            IGameDataService gameDataService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _startupValidationService = startupValidationService ?? throw new ArgumentNullException(nameof(startupValidationService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }

        public OperationResult<AppStartupResult> Initialize()
        {
            try
            {
                _loggingService.Info(nameof(AppStartupCoordinator), "Application startup initialization beginning.");

                var statusMessages = new List<string>();

                var ensureDirectoriesResult = _pathService.EnsureDirectoryStructure();
                statusMessages.Add(ensureDirectoriesResult.UserMessage);

                if (!ensureDirectoriesResult.IsSuccess)
                {
                    return OperationResult<AppStartupResult>.Fail(
                        userMessage: "The application could not prepare its required folders.",
                        failureCategory: ensureDirectoriesResult.FailureCategory,
                        technicalMessage: ensureDirectoriesResult.TechnicalMessage,
                        exception: ensureDirectoriesResult.Exception);
                }

                var emulatorProfilesPath = GetExistingEmulatorProfilePath();
                var gamesConfigPath = _pathService.GetGamesConfigPath();

                var emulatorProfiles = LoadEmulatorProfiles(emulatorProfilesPath, statusMessages);

                var validationResult = _startupValidationService.Validate(new StartupValidationOptions
                {
                    GamesConfigPath = gamesConfigPath,
                    RequiredDirectories = new[]
                    {
                        _pathService.DataDirectory,
                        _pathService.ConfigDirectory,
                        _pathService.LogsDirectory
                    },
                    OptionalDirectories = new[]
                    {
                        _pathService.AssetsDirectory
                    },
                    RequiredFiles = Array.Empty<string>(),
                    OptionalFiles = new[]
                    {
                        emulatorProfilesPath,
                        gamesConfigPath,
                        _pathService.GetAppSettingsPath()
                    },
                    EmulatorProfiles = emulatorProfiles
                });

                if (!validationResult.IsSuccess)
                {
                    statusMessages.Add(validationResult.UserMessage);
                    return OperationResult<AppStartupResult>.Fail(
                        userMessage: "Startup validation failed unexpectedly.",
                        failureCategory: validationResult.FailureCategory,
                        technicalMessage: validationResult.TechnicalMessage,
                        exception: validationResult.Exception);
                }

                statusMessages.Add(validationResult.Data?.Summary ?? "Startup validation completed.");

                GameDataLoadResult? gameData = null;
                if (File.Exists(gamesConfigPath))
                {
                    var gameLoadResult = _gameDataService.LoadGames(gamesConfigPath);
                    statusMessages.Add(gameLoadResult.UserMessage);

                    if (gameLoadResult.IsSuccess)
                    {
                        gameData = gameLoadResult.Data;
                    }
                    else
                    {
                        _loggingService.Warning(nameof(AppStartupCoordinator), "Game data load failed during startup.", gameLoadResult.TechnicalMessage);
                    }
                }
                else
                {
                    statusMessages.Add("Game data file not present yet.");
                }

                var canContinue = DetermineCanContinue(validationResult.Data, gameData);
                var startupResult = new AppStartupResult
                {
                    CanContinue = canContinue,
                    StatusMessages = statusMessages,
                    ValidationReport = validationResult.Data,
                    EmulatorProfiles = emulatorProfiles,
                    GameData = gameData
                };

                _loggingService.Info(
                    nameof(AppStartupCoordinator),
                    canContinue ? "Startup initialization completed successfully." : "Startup initialization completed with blocking issues.",
                    $"CanContinue: {canContinue} | EmulatorProfiles: {emulatorProfiles.Count} | GamesLoaded: {gameData?.Games.Count ?? 0}");

                return OperationResult<AppStartupResult>.Success(
                    startupResult,
                    canContinue ? "Startup initialization completed." : "Startup initialization completed, but blocking issues were found.");
            }
            catch (Exception ex)
            {
                _loggingService.Critical(nameof(AppStartupCoordinator), "Startup initialization failed unexpectedly.", ex, ex.Message);
                return OperationResult<AppStartupResult>.Fail(
                    userMessage: "The application could not complete startup initialization.",
                    failureCategory: FailureCategory.Unexpected,
                    technicalMessage: ex.Message,
                    exception: ex);
            }
        }

        private string GetExistingEmulatorProfilePath()
        {
            var modern = _pathService.GetEmulatorProfilesPath();
            if (File.Exists(modern))
            {
                return modern;
            }

            var legacy = _pathService.GetLegacyEmulatorsPath();
            return legacy;
        }

        private List<EmulatorProfile> LoadEmulatorProfiles(string emulatorProfilesPath, ICollection<string> statusMessages)
        {
            if (!File.Exists(emulatorProfilesPath))
            {
                statusMessages.Add("Emulator profile file not present yet.");
                _loggingService.Warning(nameof(AppStartupCoordinator), "Emulator profile file was not found during startup.", emulatorProfilesPath);
                return new List<EmulatorProfile>();
            }

            try
            {
                var json = File.ReadAllText(emulatorProfilesPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    statusMessages.Add("Emulator profile file is empty.");
                    _loggingService.Warning(nameof(AppStartupCoordinator), "Emulator profile file was empty.", emulatorProfilesPath);
                    return new List<EmulatorProfile>();
                }

                var profiles = JsonSerializer.Deserialize<List<EmulatorProfile>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                }) ?? new List<EmulatorProfile>();

                statusMessages.Add($"Loaded {profiles.Count} emulator profiles.");
                _loggingService.Info(nameof(AppStartupCoordinator), "Emulator profiles loaded.", $"Count: {profiles.Count} | Path: {emulatorProfilesPath}");
                return profiles;
            }
            catch (Exception ex)
            {
                statusMessages.Add("Failed to load emulator profiles.");
                _loggingService.Error(nameof(AppStartupCoordinator), "Failed to load emulator profiles.", ex, ex.Message);
                return new List<EmulatorProfile>();
            }
        }

        private static bool DetermineCanContinue(StartupValidationReport? validationReport, GameDataLoadResult? gameData)
        {
            if (validationReport == null || !validationReport.IsValid)
            {
                return false;
            }

            if (gameData == null)
            {
                return true;
            }

            var hasBlockingGameIssues = gameData.Issues.Any(i =>
                i.FailureCategory == FailureCategory.Configuration ||
                i.FailureCategory == FailureCategory.Validation ||
                i.FailureCategory == FailureCategory.MissingFile ||
                i.FailureCategory == FailureCategory.MissingDirectory);

            return !hasBlockingGameIssues;
        }
    }
}
