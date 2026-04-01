using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Represents the outcome of a game launch attempt.
/// 
/// Provides structured success/failure reporting instead of relying on exceptions.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class LaunchResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ExecutablePath { get; init; }
    public string? Arguments { get; init; }

    /// <summary>
    /// Creates a successful launch result.
    /// </summary>
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

    /// <summary>
    /// Creates a failed launch result.
    /// </summary>
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
