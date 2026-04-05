param(
    [string]$ProjectRoot = ".",
    [string]$OutputFile = "organization-discovery-report.md"
)

function Resolve-ProjectRoot {
    param([string]$Root)

    if (Test-Path (Join-Path $Root "ArcadeFrontend.csproj")) {
        return (Resolve-Path $Root).Path
    }

    $nested = Join-Path $Root "ArcadeFrontend"
    if (Test-Path (Join-Path $nested "ArcadeFrontend.csproj")) {
        return (Resolve-Path $nested).Path
    }

    throw "Could not find ArcadeFrontend.csproj from '$Root'. Run this from the repo root or the project folder."
}

$project = Resolve-ProjectRoot -Root $ProjectRoot
Set-Location $project

$files = Get-ChildItem -Recurse -File -Filter *.cs |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    Sort-Object FullName

$reportPath = Join-Path $project $OutputFile

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# ArcadeFrontend Organization Discovery Report")
$lines.Add("")
$lines.Add("Generated from: `" + $project + "`")
$lines.Add("")
$lines.Add("## Current C# files")
$lines.Add("")

foreach ($file in $files) {
    $relative = [System.IO.Path]::GetRelativePath($project, $file.FullName)
    $lines.Add("- `" + $relative.Replace("\","/") + "`")
}

$lines.Add("")
$lines.Add("## Suggested grouping buckets")
$lines.Add("")
$lines.Add("- Models/Audio")
$lines.Add("- Models/Configuration")
$lines.Add("- Models/UI")
$lines.Add("- Models/Visual")
$lines.Add("- Services/Audio")
$lines.Add("- Services/Behavior")
$lines.Add("- Services/Configuration")
$lines.Add("- Services/Diagnostics")
$lines.Add("- Services/Input")
$lines.Add("- Services/Launching")
$lines.Add("- Services/Library")
$lines.Add("- Services/Media")
$lines.Add("- Services/Navigation")
$lines.Add("- Services/Persistence")
$lines.Add("- Services/Sessions")
$lines.Add("- Services/State")
$lines.Add("- Services/Visual")
$lines.Add("- ViewModels/Shell")
$lines.Add("")
$lines.Add("## Next step")
$lines.Add("")
$lines.Add("Use this report as the source of truth for a phase-2 reorganization pass.")
$lines.Add("That pass should be generated from the real file list above, not from assumptions.")

Set-Content -Path $reportPath -Value $lines -Encoding UTF8

Write-Host "Discovery report created:"
Write-Host $reportPath
