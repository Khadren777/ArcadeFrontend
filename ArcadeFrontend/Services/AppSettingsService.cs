using System;
using System.IO;
using System.Text.Json;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public interface IAppSettingsService
    {
        AppSettings Current { get; }
        AppSettings Load();
        void Save(AppSettings settings);
    }

    public sealed class AppSettingsService : IAppSettingsService
    {
        private readonly IPathService _pathService;
        private readonly ILoggingService _loggingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public AppSettings Current { get; private set; } = new();

        public AppSettingsService(IPathService pathService, ILoggingService loggingService)
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

        public AppSettings Load()
        {
            var path = _pathService.GetAppSettingsPath();

            try
            {
                if (!File.Exists(path))
                {
                    Current = new AppSettings();
                    Save(Current);
                    _loggingService.Warning(nameof(AppSettingsService), "App settings file not found. Default settings created.", path);
                    return Current;
                }

                var json = File.ReadAllText(path);
                Current = string.IsNullOrWhiteSpace(json)
                    ? new AppSettings()
                    : (JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings());

                _loggingService.Info(nameof(AppSettingsService), "App settings loaded.", path);
                return Current;
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(AppSettingsService), "Failed to load app settings. Falling back to defaults.", ex, path);
                Current = new AppSettings();
                return Current;
            }
        }

        public void Save(AppSettings settings)
        {
            Current = settings ?? new AppSettings();
            var path = _pathService.GetAppSettingsPath();
            var json = JsonSerializer.Serialize(Current, _jsonOptions);
            File.WriteAllText(path, json);
            _loggingService.Info(nameof(AppSettingsService), "App settings saved.", path);
        }
    }
}
