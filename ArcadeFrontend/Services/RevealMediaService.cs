using System.IO;

/// <summary>
/// Resolves and validates reveal media paths.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class RevealMediaService
{
    private readonly PathService _pathService;

    public RevealMediaService(PathService pathService)
    {
        _pathService = pathService;
    }

    public string ResolveRevealVideoPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        string resolved = _pathService.Resolve(configuredPath);
        return File.Exists(resolved) ? resolved : string.Empty;
    }
}
