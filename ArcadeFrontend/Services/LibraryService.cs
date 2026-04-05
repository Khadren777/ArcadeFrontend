using ArcadeFrontend.Models;

/// <summary>
/// Provides loaded library data for games and emulator profiles.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class LibraryService
{
    private readonly GameDataService _gameDataService;
    private readonly List<Game> _games = new();
    private readonly List<EmulatorProfile> _emulatorProfiles = new();

    public LibraryService(GameDataService gameDataService)
    {
        _gameDataService = gameDataService;
    }

    public IReadOnlyList<Game> Games => _games;
    public IReadOnlyList<EmulatorProfile> EmulatorProfiles => _emulatorProfiles;

    public void Load()
    {
        _games.Clear();
        _games.AddRange(_gameDataService.LoadGames());

        _emulatorProfiles.Clear();
        _emulatorProfiles.AddRange(_gameDataService.LoadEmulators());
    }

    public void Reload()
    {
        Load();
    }
}
