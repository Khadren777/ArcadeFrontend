using System.Collections.Generic;

/// <summary>
/// Stores persistent lightweight UI state for the frontend.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class UiStateSnapshot
{
    public Dictionary<string, int> SelectedIndexByScreen { get; set; } = new();
    public string LastSelectedSystem { get; set; } = string.Empty;
}
