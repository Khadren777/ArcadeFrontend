using System.IO;
using System.Text.Json;
using ArcadeFrontend.Models;

/// <summary>
/// Loads and saves application settings.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class SettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService(string baseDirectory)
    {
        _settingsFilePath = Path.Combine(baseDirectory, "config", "settings.json");
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            AppSettings defaults = new();
            SaveSettings(defaults);
            return defaults;
        }

        string json = File.ReadAllText(_settingsFilePath);
        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return settings ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(_settingsFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsFilePath, json);
    }
}
