using System;
using System.Collections.Generic;
using ArcadeFrontend.Services;

namespace ArcadeFrontend.Services
{
    public static class MainWindowConfigurator
    {
        public static void Configure(
            IInputAbstractionService inputService,
            IIdleService idleService,
            IInputComboService inputComboService,
            IAttractModeCoordinator attractModeCoordinator,
            IAppSettingsService appSettingsService,
            RevealMediaService revealMediaService,
            ILoggingService loggingService)
        {
            // Register input combos
            var combos = new List<InputComboDefinition>
            {
                // Admin unlock combo: Up Up Down Down Left Right Left Right
                new InputComboDefinition
                {
                    Key = "AdminUnlock",
                    DisplayName = "Admin Unlock",
                    Sequence = new[] { InputAction.Up, InputAction.Up, InputAction.Down, InputAction.Down, InputAction.Left, InputAction.Right, InputAction.Left, InputAction.Right },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                },
                // Attract mode toggle combo: Start Select Start Select
                new InputComboDefinition
                {
                    Key = "AttractModeToggle",
                    DisplayName = "Attract Mode Toggle",
                    Sequence = new[] { InputAction.Start, InputAction.Select, InputAction.Start, InputAction.Select },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                }
            };

            inputComboService.RegisterCombos(combos);

            // Subscribe to combo matched events
            inputComboService.ComboMatched += (sender, e) =>
            {
                switch (e.ComboKey)
                {
                    case "AdminUnlock":
                        // Trigger admin action
                        inputService.RegisterExternalInput(InputAction.Admin, "Admin combo matched");
                        loggingService.Info(nameof(MainWindowConfigurator), "Admin unlock combo matched.");
                        break;
                    case "AttractModeToggle":
                        if (attractModeCoordinator.IsAttractModeActive)
                        {
                            attractModeCoordinator.ForceExitAttractMode("Attract mode toggle combo");
                        }
                        else
                        {
                            attractModeCoordinator.ForceEnterAttractMode("Attract mode toggle combo");
                        }
                        loggingService.Info(nameof(MainWindowConfigurator), "Attract mode toggle combo matched.");
                        break;
                }
            };

            // Configure idle service with settings
            var idleOptions = new IdleServiceOptions
            {
                AttractModeDelay = TimeSpan.FromSeconds(appSettingsService.Current.AttractModeTimeoutSeconds),
                HeartbeatInterval = TimeSpan.FromSeconds(1),
                StartInAttractMode = false,
                AutoEnterAttractMode = true
            };
            idleService.Configure(idleOptions);

            // Subscribe to input events to process combos
            inputService.InputReceived += (sender, e) => inputComboService.ProcessInput(e);
        }
    }
}