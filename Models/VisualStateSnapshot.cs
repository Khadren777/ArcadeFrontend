/// <summary>
/// Represents the visual state to render for the current frontend screen.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class VisualStateSnapshot
{
    public string BackgroundImagePath { get; init; } = string.Empty;
    public string AttractVideoPath { get; init; } = string.Empty;
    public bool ShowAttractVideo { get; init; }
    public bool ShowDiagnosticsPanel { get; init; }
    public string SubtitleText { get; init; } = string.Empty;
}
