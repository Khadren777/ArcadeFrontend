using System;
using System.Collections.Generic;
using System.IO;
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

    public interface IAppStartupCoordinator { OperationResult<AppStartupResult> Initialize(); }

    public sealed class AppStartupCoordinator : IAppStartupCoordinator
    {
        private readonly IPathService _pathService;
        private readonly ILoggingService _loggingService;
        private readonly IStartupValidationService _startupValidationService;
        private readonly IGameDataService _gameDataService;

        public AppStartupCoordinator(IPathService pathService, ILoggingService loggingService, IStartupValidationService startupValidationService, IGameDataService gameDataService)
        {
            _pathService = pathService; _loggingService = loggingService; _startupValidationService = startupValidationService; _gameDataService = gameDataService;
        }

        public OperationResult<AppStartupResult> Initialize()
        {
            try
            {
                var status = new List<string>();
                var ensure = _pathService.EnsureDirectoryStructure();
                status.Add(ensure.UserMessage);
                if (!ensure.IsSuccess) return OperationResult<AppStartupResult>.Fail("The application could not prepare its required folders.", ensure.FailureCategory, ensure.TechnicalMessage, ensure.Exception);

                var emulatorProfilesPath = _pathService.GetEmulatorProfilesPath();
                var gamesConfigPath = _pathService.GetGamesConfigPath();
                var emulatorProfiles = LoadEmulatorProfiles(emulatorProfilesPath, status);

                var validation = _startupValidationService.Validate(new StartupValidationOptions
                {
                    GamesConfigPath = gamesConfigPath,
                    RequiredDirectories = new[] { _pathService.DataDirectory, _pathService.ConfigDirectory, _pathService.LogsDirectory },
                    OptionalDirectories = new[] { _pathService.AssetsDirectory },
                    EmulatorProfiles = emulatorProfiles
                });
                if (!validation.IsSuccess) return OperationResult<AppStartupResult>.Fail("Startup validation failed unexpectedly.", validation.FailureCategory, validation.TechnicalMessage, validation.Exception);

                status.Add(validation.Data?.Summary ?? "Startup validation completed.");

                GameDataLoadResult? gameData = null;
                if (File.Exists(gamesConfigPath))
                {
                    var load = _gameDataService.LoadGames(gamesConfigPath);
                    status.Add(load.UserMessage);
                    if (load.IsSuccess) gameData = load.Data;
                }
                else
                {
                    status.Add("Game data file not present yet.");
                }

                var canContinue = validation.Data?.IsValid ?? false;
                return OperationResult<AppStartupResult>.Success(new AppStartupResult
                {
                    CanContinue = canContinue,
                    StatusMessages = status,
                    ValidationReport = validation.Data,
                    EmulatorProfiles = emulatorProfiles,
                    GameData = gameData
                }, canContinue ? "Startup initialization completed." : "Startup initialization completed, but blocking issues were found.");
            }
            catch (Exception ex)
            {
                return OperationResult<AppStartupResult>.Fail("The application could not complete startup initialization.", FailureCategory.Unexpected, ex.Message, ex);
            }
        }

        private List<EmulatorProfile> LoadEmulatorProfiles(string path, ICollection<string> status)
        {
            if (!File.Exists(path)) { status.Add("Emulator profile file not present yet."); return new List<EmulatorProfile>(); }
            try
            {
                var json = File.ReadAllText(path);
                var profiles = JsonSerializer.Deserialize<List<EmulatorProfile>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip }) ?? new List<EmulatorProfile>();
                status.Add($"Loaded {profiles.Count} emulator profiles.");
                return profiles;
            }
            catch
            {
                status.Add("Failed to load emulator profiles.");
                return new List<EmulatorProfile>();
            }
        }
    }
}
