using System;
using System.Diagnostics;
using System.IO;

namespace ArcadeFrontend.Services
{
    public sealed class RevealMediaService
    {
        private readonly IPathService _pathService;
        private readonly ILoggingService _loggingService;

        public RevealMediaService(IPathService pathService, ILoggingService loggingService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public string? ResolveRevealPath(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return File.Exists(configuredPath) ? configuredPath : null;
            }

            var assetPath = _pathService.ResolveInAssets(configuredPath);
            if (File.Exists(assetPath))
            {
                return assetPath;
            }

            var rootPath = _pathService.Resolve(configuredPath);
            return File.Exists(rootPath) ? rootPath : null;
        }

        public bool TryLaunchRevealMedia(string? configuredPath)
        {
            var resolvedPath = ResolveRevealPath(configuredPath);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                _loggingService.Warning(nameof(RevealMediaService), "Reveal media could not be resolved.", configuredPath);
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = resolvedPath,
                    UseShellExecute = true
                });

                _loggingService.Info(nameof(RevealMediaService), "Reveal media launched.", resolvedPath);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(RevealMediaService), "Reveal media failed to launch.", ex, resolvedPath);
                return false;
            }
        }
    }
}