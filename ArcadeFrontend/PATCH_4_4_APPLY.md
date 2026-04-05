# Patch v4.4 Apply Notes

Normalize runtime config pathing, log exact runtime paths, and provide minimal known-good config files.

Apply by overwriting:
- ArcadeFrontend.csproj
- App.xaml.cs
- Services/PathService.cs
- Services/AppStartupCoordinator.cs
- Config/games.json
- Config/emulatorProfiles.json
- Config/appsettings.json
