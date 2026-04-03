using System;
using System.IO;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public interface IPathService
    {
        string AppRootPath { get; }
        string DataDirectory { get; }
        string ConfigDirectory { get; }
        string LogsDirectory { get; }
        string AssetsDirectory { get; }
        string GetGamesConfigPath();
        string GetEmulatorProfilesPath();
        OperationResult EnsureDirectoryStructure();
    }

    public sealed class PathService : IPathService
    {
        public string AppRootPath { get; }
        public string DataDirectory { get; }
        public string ConfigDirectory { get; }
        public string LogsDirectory { get; }
        public string AssetsDirectory { get; }

        public PathService(string appRootPath, ILoggingService? loggingService = null)
        {
            AppRootPath = Path.GetFullPath(appRootPath);
            DataDirectory = Path.Combine(AppRootPath, "Data");
            ConfigDirectory = Path.Combine(AppRootPath, "Config");
            LogsDirectory = Path.Combine(AppRootPath, "Logs");
            AssetsDirectory = Path.Combine(AppRootPath, "Assets");
        }

        public string GetGamesConfigPath() => Path.Combine(ConfigDirectory, "games.json");
        public string GetEmulatorProfilesPath() => Path.Combine(ConfigDirectory, "emulatorProfiles.json");

        public OperationResult EnsureDirectoryStructure()
        {
            try
            {
                Directory.CreateDirectory(AppRootPath);
                Directory.CreateDirectory(DataDirectory);
                Directory.CreateDirectory(ConfigDirectory);
                Directory.CreateDirectory(LogsDirectory);
                Directory.CreateDirectory(AssetsDirectory);
                return OperationResult.Success("Directory structure is ready.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("The application could not prepare its folders.", FailureCategory.Unexpected, ex.Message, ex);
            }
        }
    }
}
