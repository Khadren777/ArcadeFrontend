# Patch v4.2 Apply Notes

This patch fixes the immediate usability problem:
- adds an Exit button
- adds a usable empty-state message when no games are loaded
- makes Back exit the app from the main screen when no game is running
- keeps diagnostics accessible

Apply by overwriting:
- MainWindow.xaml
- ViewModels/MainViewModel.cs

Then rebuild and run.
