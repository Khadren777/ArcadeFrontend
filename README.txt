Arcade Frontend Clean Package

This is a reconstructed clean-structure overwrite bundle built from the files generated in chat/canvas.

Contents
- Combo-integrated startup path
- MainWindow UI shell
- Core launch / diagnostics / idle / navigation / input services
- MainViewModel
- Clean folder structure

Important
- Expect a compile-fix pass when merging into your real repo.
- This package is intended to get you much closer, not guarantee zero cleanup.
- Back up your existing project before overwriting.

Recommended first run
1. Back up repo.
2. Replace files carefully.
3. Build once and fix any namespace/project include mismatches.
4. Point Config\games.json and Config\emulatorProfiles.json to your real data.
5. Test one fake launch first (Notepad, etc.).
