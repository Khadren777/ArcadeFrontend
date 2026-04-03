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

        string Resolve(params string[] segments);
        string ResolveInAssets(params string[] segments);
        string ResolveInConfig(params string[] segments);
        string ResolveInData(params string[] segments);

        string GetGamesConfigPath();
        string GetEmulatorProfilesPath();
        string GetLogFilePath(DateTime? utcDate = null);
        string GetAssetPath(string relativePath);
        string GetDataPath(string relativePath);
        string GetConfigPath(string relativePath);
        OperationResult EnsureDirectoryStructure();
    }

    public sealed class PathService : IPathService
    {
        private readonly ILoggingService? _loggingService;

        public string AppRootPath { get; }
        public string DataDirectory { get; }
        public string ConfigDirectory { get; }
        public string LogsDirectory { get; }
        public string AssetsDirectory { get; }

        public PathService(string appRootPath, ILoggingService? loggingService = null)
        {
            if (string.IsNullOrWhiteSpace(appRootPath))
            {
                throw new ArgumentException("Application root path cannot be null or whitespace.", nameof(appRootPath));
            }

            AppRootPath = Path.GetFullPath(appRootPath);
            _loggingService = loggingService;

            DataDirectory = Path.Combine(AppRootPath, "Data");
            ConfigDirectory = Path.Combine(AppRootPath, "Config");
            LogsDirectory = Path.Combine(AppRootPath, "Logs");
            AssetsDirectory = Path.Combine(AppRootPath, "Assets");
        }

        public string Resolve(params string[] segments)
        {
            return CombineValidatedPath(AppRootPath, segments, nameof(segments));
        }

        public string ResolveInAssets(params string[] segments)
        {
            return CombineValidatedPath(AssetsDirectory, segments, nameof(segments));
        }

        public string ResolveInConfig(params string[] segments)
        {
            return CombineValidatedPath(ConfigDirectory, segments, nameof(segments));
        }

        public string ResolveInData(params string[] segments)
        {
            return CombineValidatedPath(DataDirectory, segments, nameof(segments));
        }

        public string GetGamesConfigPath()
        {
            return Path.Combine(ConfigDirectory, "games.json");
        }

        public string GetEmulatorProfilesPath()
        {
            return Path.Combine(ConfigDirectory, "emulatorProfiles.json");
        }

        public string GetLogFilePath(DateTime? utcDate = null)
        {
            var date = (utcDate ?? DateTime.UtcNow).ToString("yyyyMMdd");
            return Path.Combine(LogsDirectory, $"arcade-{date}.log");
        }

        public string GetAssetPath(string relativePath)
        {
            return CombineValidatedPath(AssetsDirectory, new[] { relativePath }, nameof(relativePath));
        }

        public string GetDataPath(string relativePath)
        {
            return CombineValidatedPath(DataDirectory, new[] { relativePath }, nameof(relativePath));
        }

        public string GetConfigPath(string relativePath)
        {
            return CombineValidatedPath(ConfigDirectory, new[] { relativePath }, nameof(relativePath));
        }

        public OperationResult EnsureDirectoryStructure()
        {
            try
            {
                Directory.CreateDirectory(AppRootPath);
                Directory.CreateDirectory(DataDirectory);
                Directory.CreateDirectory(ConfigDirectory);
                Directory.CreateDirectory(LogsDirectory);
                Directory.CreateDirectory(AssetsDirectory);

                _loggingService?.Info(
                    nameof(PathService),
                    "Directory structure ensured.",
                    $"Root: {AppRootPath} | Data: {DataDirectory} | Config: {ConfigDirectory} | Logs: {LogsDirectory} | Assets: {AssetsDirectory}");

                return OperationResult.Success("Directory structure is ready.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService?.Error(nameof(PathService), "Access denied while creating directory structure.", ex, ex.Message);
                return OperationResult.Fail(
                    userMessage: "The application could not prepare its folders because access was denied.",
                    failureCategory: FailureCategory.Unauthorized,
                    technicalMessage: ex.Message,
                    exception: ex);
            }
            catch (Exception ex)
            {
                _loggingService?.Critical(nameof(PathService), "Unexpected error while creating directory structure.", ex, ex.Message);
                return OperationResult.Fail(
                    userMessage: "The application could not prepare its folders.",
                    failureCategory: FailureCategory.Unexpected,
                    technicalMessage: ex.Message,
                    exception: ex);
            }
        }

        private static string CombineValidatedPath(string baseDirectory, string[] segments, string parameterName)
        {
            if (segments == null || segments.Length == 0)
            {
                throw new ArgumentException("Path segments cannot be null or empty.", parameterName);
            }

            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new ArgumentException("Path segments cannot contain null or whitespace values.", parameterName);
                }
            }

            var parts = new string[segments.Length + 1];
            parts[0] = baseDirectory;
            Array.Copy(segments, 0, parts, 1, segments.Length);

            var combined = Path.GetFullPath(Path.Combine(parts));
            var baseFullPath = Path.GetFullPath(baseDirectory);

            if (!combined.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The resolved path escapes the intended base directory.");
            }

            return combined;
        }
    }
}
