using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Transitional root view model for the application.
/// 
/// Wraps MainWindowViewModel so we can incrementally migrate logic
/// without breaking the application in a single refactor pass.
/// </summary>
namespace ArcadeFrontend.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes the shell with the existing main view model.
    /// </summary>
    public ShellViewModel(MainWindowViewModel main)
    {
        Main = main;
    }

    public MainWindowViewModel Main { get; }
}
