# Arcade Frontend Patch v3 - Consolidation Overwrite

This patch is an aggressive consolidation pass based on the current repo state and the missing items discussed in chat.

## Biggest fixes in this patch
1. App root now uses the executable/output folder instead of LocalAppData
2. Logs are written under the app folder so they can be accessed by the app/cabinet
3. PathService gains general Resolve helpers
4. Startup config pathing is aligned to the actual output-copy behavior
5. Emulator profile loading now supports both:
   - config/emulatorProfiles.json
   - config/emulators.json
6. Reveal combo is wired to real media launching if configured
7. Logs folder can be opened from inside the app through a combo
8. App settings service is added and injected
9. ScreenType enum is added for visual-state compatibility
10. csproj content-copy rules are updated to match the current architecture

## Recommended apply order
- overwrite the files in this patch
- rebuild
- ensure your config files are present in ArcadeFrontend/bin/.../config/
- test:
  - game visibility
  - reveal combo
  - logs combo
  - one fake launch
  - one real emulator launch

## Included combos
- admin-open: Up Up Down Down Select
- reveal-video: Left Right Left Right Start
- toggle-attract: Back Back Start
- open-logs: Admin Admin Back
