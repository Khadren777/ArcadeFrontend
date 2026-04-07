# Patch v4.6 Apply Notes

This is a menu-behavior patch for the current minimalist shell.

## What it changes
- makes the Main Menu feel more like a real frontend entry point
- adds top-level menu options:
  - Systems
  - Games
  - Diagnostics
  - Exit
- adds contextual right-panel content for menu selections
- makes Back return to Main Menu from submenus
- keeps Exit available from Main Menu
- improves empty-state messaging when no games are loaded

## Apply by overwriting
- MainWindow.xaml
- ViewModels/MainViewModel.cs

## Notes
This patch is intentionally focused on menu behavior only.
It does not yet rebuild the full cabinet UX, but it gives you a much better next baseline.
