# 🎮 Arcade Frontend

A custom Windows-based arcade cabinet frontend built in C# (.NET, WPF), designed for a multi-player arcade system with support for emulation, native PC games, and custom hardware.

---

## 🎯 Project Goals

- Provide a console-like arcade experience
- Support multi-system emulation + native PC games
- Enable controller-only navigation (no keyboard required once deployed)
- Maintain clean, modular architecture
- Integrate seamlessly with custom cabinet hardware

---

## 🖥️ Hardware Specs

### PC
- **Machine**: Beelink SER5 Mini PC
- **OS**: Windows 11 Pro
- **CPU**: AMD Ryzen 5 5500U (6C/12T, up to 4 GHz)
- **RAM**: 16GB DDR4
- **Storage**: 500GB NVMe SSD
- **GPU**: AMD Radeon integrated (6 cores)
- **Networking**: WiFi 6, 2.5G LAN, Bluetooth 5.2
- **Display output**: HDMI, DisplayPort, USB-C (triple display capable)
- **Network status**: WiFi and Bluetooth will be active in cabinet

### Display
- **Model**: Sanyo DP42840
- **Connection**: HDMI
- **Notes**: 42" 1080p TV; treated as primary display in cabinet

### Input Hardware
- **Encoders**: SJ@JX Zero Delay USB Encoders
  - Present to Windows as **DirectInput gamepad devices** (not keyboard)
  - Each player = independent USB device
  - ⚠️ Gamepad polling not yet implemented — keyboard bindings exist but DirectInput support is still needed (SharpDX.DirectInput recommended)
- **Light Guns**: Dual Sinden Light Guns
  - Run as a separate background process
  - Present as **mouse input** to Windows
  - Integration approach with frontend not yet decided
- **Planned**: Trackball, foot pedals (industrial switches)

### Player Layout
- P1 / P2 → 6-button (Capcom layout)
- P3 / P4 → 4-button layout
- P5 / P6 → modular expansion wings (same layout as P3/P4)

### Audio
- **Amp**: ZK-TB21 2.1 Channel Bluetooth Amp (~50W per channel + subwoofer)
- **Inputs**: Bluetooth, AUX
- **Output**: Stereo L/R + dedicated subwoofer channel
- **Design**: Front-facing, full-range stereo + sub, tight cabinet enclosure

---

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET (matching project target)
- Visual Studio (recommended)

### 1. Clone Repo
```bash
git clone https://github.com/Khadren777/ArcadeFrontend.git
cd ArcadeFrontend
```

### 2. Build
```bash
dotnet build
dotnet run
```

### 3. Create Config Folder
Inside your output directory:
```
config/
```

### 4. Add games.json
```json
{
  "games": [
    {
      "id": "notepad_test",
      "title": "Notepad Test",
      "platform": "PC",
      "launchTarget": "notepad",
      "executablePathOverride": "C:\\Windows\\System32\\notepad.exe",
      "isEnabled": true,
      "isHidden": false,
      "description": "Quick startup test"
    }
  ]
}
```

### 5. (Optional) Emulator Profile
```json
[
  {
    "key": "mame",
    "executablePath": "D:\\Arcade\\Emulators\\MAME\\mame.exe",
    "arguments": "{rom}",
    "workingDirectory": "D:\\Arcade\\Emulators\\MAME"
  }
]
```

### 6. Run
Navigate → Games → Launch → Notepad Test

### 7. Keyboard Fallback (dev only)
| Key | Action |
|-----|--------|
| Arrow Keys | Navigate |
| Enter | Select |
| Space | Start |
| Escape | Back / Exit |
| F1 | Admin |

---

## 🧠 Architecture Overview

### UI Layer
- `MainWindow.xaml` / `MainWindow.xaml.cs`

### ViewModel Layer
- `MainViewModel.cs` — navigation state, menu flow, game selection, commands

### Services

**Core**
- `GameLauncherService`
- `GameDataService`
- `PathService`
- `LoggingService`
- `NavigationStateService`

**Input**
- `InputAbstractionService`
- `InputComboService`

**System**
- `IdleService`
- `AttractModeCoordinator`
- `AppStartupCoordinator`

**Diagnostics**
- `DiagnosticsSummaryBuilder`

### Models
- `GameDefinition`
- `EmulatorProfile`
- `LaunchResult`
- `NavigationStateSnapshot`

---

## 🧭 Application Flow

```
Startup → AppStartupCoordinator → Load Config → Main Menu → User Input → Navigation → Launch Game
```

---

## 🎮 Input System Notes

- The `IInputAbstractionService` uses an `InputAction` enum as the single abstraction for all input sources
- Keyboard bindings are registered in `ConfigureInputBindings()` — these work for dev/testing
- Input combos (e.g. admin access, attract mode toggle) are registered via `IInputComboService`
- **⚠️ DirectInput gamepad polling is not yet implemented** — required for SJ@JX encoders
- **⚠️ Sinden gun integration approach not yet decided** — guns present as mouse input via Sinden software

---

## ⏱️ Attract Mode

- Idle timer currently set to **5 minutes** (`AttractModeDelay = TimeSpan.FromMinutes(5)`)
- This value should be increased before production deployment
- Can be toggled via combo: Back → Back → Start

---

## 📁 Project Structure

```
ArcadeFrontend/
├── Models/
├── Services/
├── ViewModels/
├── Infrastructure/
├── Assets/
├── config/
├── Logs/
├── MainWindow.xaml
├── App.xaml
```

---

## ⚙️ Configuration

- `games.json` — defines available games
- `emulatorProfiles.json` — defines emulator execution paths and arguments

---

## 📦 Logging

Logs stored in `/Logs/arcade-YYYYMMDD.log`

Includes: startup validation, game launches, errors, diagnostics

---

## 🐛 Known Issues / Technical Debt

| Issue | Priority | Notes |
|-------|----------|-------|
| Silent `catch {}` in `OnClosing` | High | Swallows cleanup errors — should log via `ILoggingService` |
| Synchronous startup in `OnLoaded` | Medium | `AppStartupCoordinator.Initialize()` may block UI thread — should be async |
| No DirectInput gamepad support | High | Required for SJ@JX encoders |
| Sinden gun integration undefined | Medium | Guns present as mouse input; integration approach TBD |
| Button styling missing in XAML | Medium | Default WPF buttons clash with dark theme |
| ListBox selection highlight unstyled | Medium | Default blue highlight clashes with dark card style |
| Attract mode delay too short | Low | 5 min default; increase before production |

---

## 🚧 Current Status

### ✅ Completed
- Architecture migration
- Clean build
- Main menu system
- Logging + diagnostics
- Input abstraction layer (keyboard)
- Input combo system

### 🔄 In Progress
- ROM integration
- Controller mapping (DirectInput)
- Attract mode tuning
- Visual / UI polish

### 📌 Planned
- Systems menu
- Favorites / recent
- Admin UI
- Video previews
- Sinden gun integration
- Trackball + foot pedal support
- Shutdown option in admin menu (safe power-off before cutting cabinet power)

---

## 🧪 Development Notes

- Controller-first design — avoid keyboard dependency in production
- Config-driven behavior
- Modular services behind interfaces
- WPF + MVVM pattern
- Dependency injection via constructor throughout

---

## 🧭 Next Steps (Immediate)

1. Fix silent `catch {}` in `OnClosing`
2. Make `OnLoaded` startup async
3. Add SharpDX.DirectInput gamepad polling to `IInputAbstractionService`
4. Style buttons and ListBox selection highlight in `MainWindow.xaml`
5. Hook up MAME and validate ROM launch pipeline
6. Map cabinet controls end-to-end
7. Increase attract mode idle timer