using ArcadeFrontend.Models;
using System.IO;
using System.Text.Json;

namespace ArcadeFrontend.Services
{
    public class RecentGamesService
    {
        private readonly string _configDirectory;
        private readonly string _recentGamesPath;

        public RecentGamesService(string baseDirectory)
        {
            _configDirectory = Path.Combine(baseDirectory, "config");
            _recentGamesPath = Path.Combine(_configDirectory, "recentgames.json");
        }

        public List<Game> LoadRecentGames()
        {
            if (!File.Exists(_recentGamesPath))
            {
                return new List<Game>();
            }

            string json = File.ReadAllText(_recentGamesPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Game>();
            }

            return JsonSerializer.Deserialize<List<Game>>(json) ?? new List<Game>();
        }

        public void SaveRecentGames(List<Game> recentGames)
        {
            Directory.CreateDirectory(_configDirectory);

            string json = JsonSerializer.Serialize(recentGames, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_recentGamesPath, json);
        }
    }
}