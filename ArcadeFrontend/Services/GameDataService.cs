using System.IO;
using System.Text.Json;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public class GameDataService
    {
        private readonly string _gamesPath;
        private readonly string _emulatorsPath;

        public GameDataService(string baseDirectory)
        {
            string configDirectory = Path.Combine(baseDirectory, "config");
            _gamesPath = Path.Combine(configDirectory, "games.json");
            _emulatorsPath = Path.Combine(configDirectory, "emulators.json");
        }

        public List<Game> LoadGames()
        {
            return LoadJson<List<Game>>(_gamesPath) ?? new List<Game>();
        }

        public List<EmulatorProfile> LoadEmulators()
        {
            return LoadJson<List<EmulatorProfile>>(_emulatorsPath) ?? new List<EmulatorProfile>();
        }

        private static T? LoadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }

            string json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json);
        }

        public void SaveGames(List<Game> games)
        {
            string json = JsonSerializer.Serialize(games, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_gamesPath, json);
        }
    }
}