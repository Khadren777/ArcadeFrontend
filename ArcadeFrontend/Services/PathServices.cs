using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArcadeFrontend.Services
{
    public class PathService
    {
        public string BaseDirectory { get; }
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");
        public string EmulatorsDirectory => Path.Combine(BaseDirectory, "Emulators");
        public string GamesDirectory => Path.Combine(BaseDirectory, "Games");

        public PathService()
        {
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            path = path.Trim();

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            bool hasDirectorySeparator = path.Contains("\\") || path.Contains("/");

            if (!hasDirectorySeparator)
            {
                return path;
            }

            return Path.Combine(BaseDirectory, path);
        }
    }
}