using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public sealed class GameDataFile
    {
        public List<GameDefinition> Games { get; init; } = new();
    }

    public sealed class GameDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;
        public string LaunchTarget { get; init; } = string.Empty;
        public string? EmulatorProfileKey { get; init; }
        public string? ExecutablePathOverride { get; init; }
        public string? Arguments { get; init; }
        public string? WorkingDirectoryOverride { get; init; }
        public bool IsEnabled { get; init; } = true;
        public bool IsHidden { get; init; } = false;
        public string? Description { get; init; }
    }

    public sealed class GameValidationIssue
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? GameId { get; init; }
        public string? GameTitle { get; init; }
        public FailureCategory FailureCategory { get; init; } = FailureCategory.Validation;
        public override string ToString() => $"{Code}: {Message}";
    }

    public sealed class GameDataLoadResult
    {
        public IReadOnlyList<GameDefinition> Games { get; init; } = Array.Empty<GameDefinition>();
        public IReadOnlyList<GameValidationIssue> Issues { get; init; } = Array.Empty<GameValidationIssue>();
        public string SourcePath { get; init; } = string.Empty;
    }

    public interface IGameDataService
    {
        OperationResult<GameDataLoadResult> LoadGames(string filePath);
    }

    public sealed class GameDataService : IGameDataService
    {
        private readonly ILoggingService _loggingService;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public GameDataService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public OperationResult<GameDataLoadResult> LoadGames(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return OperationResult<GameDataLoadResult>.Fail(
                    "The game data file path was not provided.",
                    FailureCategory.Validation);
            }

            if (!File.Exists(filePath))
            {
                return OperationResult<GameDataLoadResult>.Fail(
                    "The game data file could not be found.",
                    FailureCategory.MissingFile,
                    filePath);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var dataFile = JsonSerializer.Deserialize<GameDataFile>(json, _jsonOptions) ?? new GameDataFile();
                var games = dataFile.Games ?? new List<GameDefinition>();
                var issues = ValidateGames(games);

                var result = new GameDataLoadResult
                {
                    Games = games,
                    Issues = issues,
                    SourcePath = filePath
                };

                var message = issues.Count == 0
                    ? $"Loaded {games.Count} game entries."
                    : $"Loaded {games.Count} game entries with {issues.Count} validation issue(s).";

                _loggingService.Info(
                    nameof(GameDataService),
                    "Game data loaded.",
                    $"Path: {filePath} | Games: {games.Count} | Issues: {issues.Count}");

                return OperationResult<GameDataLoadResult>.Success(result, message);
            }
            catch (Exception ex)
            {
                return OperationResult<GameDataLoadResult>.Fail(
                    "The game data file could not be loaded.",
                    FailureCategory.Unexpected,
                    ex.Message,
                    ex);
            }
        }

        private static List<GameValidationIssue> ValidateGames(IReadOnlyList<GameDefinition> games)
        {
            var issues = new List<GameValidationIssue>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < games.Count; i++)
            {
                var game = games[i];
                var entryLabel = !string.IsNullOrWhiteSpace(game.Title)
                    ? game.Title
                    : $"Entry {i + 1}";

                if (string.IsNullOrWhiteSpace(game.Id))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_ID_MISSING",
                        Message = $"{entryLabel} is missing an Id.",
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.Validation
                    });
                }
                else if (!seenIds.Add(game.Id))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_ID_DUPLICATE",
                        Message = $"{entryLabel} uses duplicate Id '{game.Id}'.",
                        GameId = game.Id,
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.Validation
                    });
                }

                if (string.IsNullOrWhiteSpace(game.Title))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_TITLE_MISSING",
                        Message = $"Entry {i + 1} is missing a Title.",
                        GameId = game.Id,
                        FailureCategory = FailureCategory.Validation
                    });
                }

                if (string.IsNullOrWhiteSpace(game.LaunchTarget))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_LAUNCH_TARGET_MISSING",
                        Message = $"{entryLabel} is missing a LaunchTarget.",
                        GameId = game.Id,
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.Validation
                    });
                }

                if (string.IsNullOrWhiteSpace(game.ExecutablePathOverride) &&
                    string.IsNullOrWhiteSpace(game.EmulatorProfileKey))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_LAUNCHER_UNRESOLVED",
                        Message = $"{entryLabel} has neither ExecutablePathOverride nor EmulatorProfileKey.",
                        GameId = game.Id,
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.Configuration
                    });
                }

                if (!string.IsNullOrWhiteSpace(game.ExecutablePathOverride) &&
                    !File.Exists(game.ExecutablePathOverride))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_EXE_OVERRIDE_MISSING",
                        Message = $"{entryLabel} references a missing executable override: {game.ExecutablePathOverride}",
                        GameId = game.Id,
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.MissingFile
                    });
                }

                if (!string.IsNullOrWhiteSpace(game.WorkingDirectoryOverride) &&
                    !Directory.Exists(game.WorkingDirectoryOverride))
                {
                    issues.Add(new GameValidationIssue
                    {
                        Code = "GAME_WORKDIR_OVERRIDE_MISSING",
                        Message = $"{entryLabel} references a missing working directory override: {game.WorkingDirectoryOverride}",
                        GameId = game.Id,
                        GameTitle = game.Title,
                        FailureCategory = FailureCategory.MissingDirectory
                    });
                }
            }

            return issues;
        }
    }
}