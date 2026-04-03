using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public sealed class GameDataFile { public List<GameDefinition> Games { get; init; } = new(); }

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

    public interface IGameDataService { OperationResult<GameDataLoadResult> LoadGames(string filePath); }

    public sealed class GameDataService : IGameDataService
    {
        private readonly ILoggingService _loggingService;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        public GameDataService(ILoggingService loggingService) { _loggingService = loggingService; }

        public OperationResult<GameDataLoadResult> LoadGames(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return OperationResult<GameDataLoadResult>.Fail("The game data file path was not provided.", FailureCategory.Validation);
            if (!File.Exists(filePath)) return OperationResult<GameDataLoadResult>.Fail("The game data file could not be found.", FailureCategory.MissingFile, filePath);
            try
            {
                var json = File.ReadAllText(filePath);
                var dataFile = JsonSerializer.Deserialize<GameDataFile>(json, _jsonOptions) ?? new GameDataFile();
                var games = dataFile.Games ?? new List<GameDefinition>();
                var issues = new List<GameValidationIssue>();
                return OperationResult<GameDataLoadResult>.Success(new GameDataLoadResult { Games = games, Issues = issues, SourcePath = filePath }, $"Loaded {games.Count} game entries.");
            }
            catch (Exception ex)
            {
                return OperationResult<GameDataLoadResult>.Fail("The game data file could not be loaded.", FailureCategory.Unexpected, ex.Message, ex);
            }
        }
    }
}
