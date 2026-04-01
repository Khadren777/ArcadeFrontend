/// <summary>
/// Represents the outcome of a game launch attempt.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class LaunchResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ExecutablePath { get; init; }
    public string? Arguments { get; init; }

    public static LaunchResult Succeeded(string message, string? executablePath = null, string? arguments = null)
    {
        return new LaunchResult
        {
            Success = true,
            Message = message,
            ExecutablePath = executablePath,
            Arguments = arguments
        };
    }

    public static LaunchResult Failed(string message, string? executablePath = null, string? arguments = null)
    {
        return new LaunchResult
        {
            Success = false,
            Message = message,
            ExecutablePath = executablePath,
            Arguments = arguments
        };
    }
}
