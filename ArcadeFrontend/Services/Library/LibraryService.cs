using ArcadeFrontend.Models;

/// <summary>
/// Provides loaded library data for games and emulator profiles.
/// </summary>
namespace ArcadeFrontend.Services.Library;

public sealed class LibraryService
{
    private readonly GameDataService _gameDataService;
    private readonly List<Game> _games = new();
    private readonly List<EmulatorProfile> _emulatorProfiles = new();

    /// <summary>
    /// Initializes the library service.
    /// </summary>
    public LibraryService(GameDataService gameDataService)
    {
        _gameDataService = gameDataService;
    }

    /// <summary>
    /// Gets the currently loaded games.
    /// </summary>
    public IReadOnlyList<Game> Games => _games;

    /// <summary>
    /// Gets the currently loaded emulator profiles.
    /// </summary>
    public IReadOnlyList<EmulatorProfile> EmulatorProfiles => _emulatorProfiles;

    /// <summary>
    /// Loads games and emulator profiles from persistence.
    /// </summary>
    public void Load()
    {
        _games.Clear();
        _games.AddRange(_gameDataService.LoadGames());

        _emulatorProfiles.Clear();
        _emulatorProfiles.AddRange(_gameDataService.LoadEmulators());
    }

    /// <summary>
    /// Reloads the library from disk.
    /// </summary>
    public void Reload()
    {
        Load();
    }
}
