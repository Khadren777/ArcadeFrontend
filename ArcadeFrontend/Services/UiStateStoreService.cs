using System.IO;
using System.Text.Json;
using ArcadeFrontend.Models;

/// <summary>
/// Loads and saves lightweight persistent UI state for the frontend.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class UiStateStoreService
{
    private readonly string _uiStateFilePath;

    public UiStateStoreService(string baseDirectory)
    {
        _uiStateFilePath = Path.Combine(baseDirectory, "config", "ui-state.json");
    }

    public UiStateSnapshot Load()
    {
        if (!File.Exists(_uiStateFilePath))
        {
            UiStateSnapshot snapshot = new();
            Save(snapshot);
            return snapshot;
        }

        string json = File.ReadAllText(_uiStateFilePath);
        UiStateSnapshot? state = JsonSerializer.Deserialize<UiStateSnapshot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return state ?? new UiStateSnapshot();
    }

    public void Save(UiStateSnapshot state)
    {
        string? directory = Path.GetDirectoryName(_uiStateFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_uiStateFilePath, json);
    }

    public int GetSelectedIndex(UiStateSnapshot state, string screenKey)
    {
        if (state.SelectedIndexByScreen.TryGetValue(screenKey, out int index))
        {
            return index;
        }

        return 0;
    }

    public void SetSelectedIndex(UiStateSnapshot state, string screenKey, int index)
    {
        state.SelectedIndexByScreen[screenKey] = index;
    }
}
