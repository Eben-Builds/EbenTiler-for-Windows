# EbenTiler for Windows

**English** | [한국어](README.ko.md)

A lightweight Windows tray utility that snaps the active window to halves, quarters, thirds, and more with keyboard shortcuts.

- No separate runtime installation required (.NET Framework 4.8 is included with Windows 10/11)
- Single executable, about 100 KB
- Runs in the notification area with fully configurable hotkeys
- Checks for new stable releases at most once per day and notifies you without automatic downloads or installation

[Privacy](PRIVACY.md) · [Security](SECURITY.md) · [Code signing policy](docs/CODE_SIGNING.md)

## Default shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + ←` | Left half |
| `Ctrl + Alt + →` | Right half |
| `Ctrl + Alt + ↑` | Top half |
| `Ctrl + Alt + ↓` | Bottom half |
| `Ctrl + Alt + U` | Top-left quarter |
| `Ctrl + Alt + I` | Top-right quarter |
| `Ctrl + Alt + J` | Bottom-left quarter |
| `Ctrl + Alt + K` | Bottom-right quarter |
| `Ctrl + Alt + D` / `F` / `H` | Left / center / right third |
| `Ctrl + Alt + E` / `T` | Left 2/3 / right 2/3 |
| `Ctrl + Alt + Enter` | Maximize |
| `Ctrl + Alt + Shift + ↑` | Vertical maximize while keeping width |
| `Ctrl + Alt + C` | Center on screen |
| `Ctrl + Alt + =` / `-` | Grow / shrink |
| `Ctrl + Alt + Backspace` | Restore the pre-snap position and size |
| `Ctrl + Alt + Shift + →` / `←` | Move to next / previous monitor |

Why not use `Ctrl + Arrow`? Most editors and browsers already use those shortcuts for word-by-word cursor movement. Taking them globally would interfere with normal typing. You can change every shortcut in Settings.

### Repeating a shortcut cycles the width

Press `Ctrl + Alt + ←` repeatedly and the left-side layout cycles through **1/2 → 1/3 → 2/3**.
The cycle only continues when the shortcut is pressed again within two seconds; otherwise it starts again from 1/2.
This behavior can be disabled in Settings.

## Installation

For normal use, download and double-click `EbenTiler-Setup.exe`.
Administrator privileges are not required, and the app installs only for the current Windows user.

- Program path: `%LOCALAPPDATA%\Programs\EbenTiler\EbenTiler.exe`
- Adds a Start menu shortcut
- Lets you choose whether EbenTiler starts with Windows
- Can launch immediately after installation
- Uninstall from **Settings > Apps > Installed apps > EbenTiler for Windows > Uninstall**

See [`INSTALL.md`](INSTALL.md) for detailed installation, installer build, and code-signing information.

For development, the PowerShell installer is also available:

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1
powershell -ExecutionPolicy Bypass -File install.ps1 -Uninstall
```

Use `-KeepConfig` when uninstalling if you want to preserve settings. Use `-NoStartup` during installation to avoid registering startup launch.

## Build

The .NET SDK is not required. EbenTiler builds with the .NET Framework 4.8 compiler available on Windows.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

The output is:

```text
build\EbenTiler.exe
```

To build the installer:

```powershell
powershell -ExecutionPolicy Bypass -File build-installer.ps1
```

Outputs:

```text
dist\EbenTiler-Setup.exe
dist\EbenTiler-Setup.exe.sha256
```

## Running EbenTiler

If you installed EbenTiler, it should already be running. To try it without installing:

```powershell
build\EbenTiler.exe
```

An icon appears in the Windows notification area. **If you do not see it, click `∧` to check the hidden tray icons.** Windows may hide icons from newly installed apps by default.

Left-clicking or right-clicking the tray icon opens the menu.

- `Hotkey settings...` opens Settings
- `Start with Windows` toggles startup registration and shows a check mark when enabled
- `Exit` closes EbenTiler

The tray menu re-reads the actual startup registration state whenever it opens, so the check mark stays in sync even if the setting changes elsewhere.

### First run

New users see a short welcome guide explaining the core shortcuts.
`Do not show again` is selected by default, so the guide normally appears once. Clear that option before closing if you want to see it again next time.

### Update checks

EbenTiler checks GitHub's public latest Release metadata at most once every 24 hours.
If a newer stable version exists, Windows shows a one-time notification.

Clicking the notification opens `Settings > About`, where you can also press `Check for updates` manually at any time.
EbenTiler does not download or install updates in the background. When a new version is available, the user chooses whether to open the GitHub Release page.

## Command line usage

EbenTiler can also be controlled from scripts or other tools.

```powershell
EbenTiler.exe --apply LeftHalf                 # Snap the active window to the left half
EbenTiler.exe --apply TopRight --hwnd 0x3B078E # Target a specific window
EbenTiler.exe --info                           # Show active-window and work-area information
EbenTiler.exe --list                           # List available commands
EbenTiler.exe --settings                       # Open Settings only
EbenTiler.exe --check                          # Check for hotkey registration conflicts
EbenTiler.exe --startup on|off|status          # Enable/disable/query startup registration
EbenTiler.exe --out result.txt --info          # Also write the result to a file
```

`--check` reports registration status like this:

```text
total=21
assigned=21
unassigned=0
failed=1
conflict=Right third (Ctrl + Alt + H)
```

Because `EbenTiler.exe` is a GUI application, standard output may not always be available through a pipe. Use `--out <file>` when a script needs to read the result reliably.

## Settings

Open the tray menu and choose the hotkey settings entry.

- Select an action on the left, then **press the key combination you want** in the input field.
- The **preview** on the right shows where the selected action will place the window.
  Actions that cannot be explained by position alone, such as resize or monitor movement, use outlines and arrows.
- If another action already uses the same shortcut, EbenTiler asks before clearing the existing assignment.
- Shortcuts without a modifier (`Ctrl`, `Alt`, `Shift`, or `Win`) are blocked because a global registration would prevent other programs from using that key normally.
- The `About` page shows the current version and update status.

## Icon

`assets\app.ico` is embedded into the executable at build time. It contains separate artwork for 16 / 24 / 32 / 48 / 64 / 128 / 256 pixel sizes.
Each size is drawn separately so the notification-area icon stays sharp instead of becoming a blurry downscaled version of one large image.

The icon represents two overlapping windows, matching the idea of arranging windows before they are tiled.
It uses blue and white only so it remains visible on dark taskbars.

```powershell
powershell -ExecutionPolicy Bypass -File tools\make-appicon.ps1   # Rebuild the icon
```

## Configuration file

Settings are stored in `%APPDATA%\EbenTiler\config.ini` and can also be edited manually.

```ini
[Hotkeys]
LeftHalf=Ctrl+Alt+Left
TopLeft=Ctrl+Alt+U
Maximize=Ctrl+Alt+Enter

[Options]
Gap=0                  ; Gap around and between windows, in pixels
CycleHalves=true       ; Repeating the same shortcut cycles 1/2 -> 1/3 -> 2/3
ShowWelcomeGuide=false ; Whether to show the welcome guide on the next launch
```

Leave a hotkey value empty to disable that action's global shortcut.

Update-check state is stored separately in `%APPDATA%\EbenTiler\update-state.ini`. It contains only the last check time and the release tag that has already been notified.

## Privacy

EbenTiler does not collect personal information, usage analytics, window titles, keyboard input, or file contents, and it does not use telemetry, advertising, or analytics SDKs.

For update notifications, it checks only the latest public release metadata from GitHub at most once every 24 hours. It does not automatically download or install update files.

See [`PRIVACY.md`](PRIVACY.md) for network behavior and locally stored information.

## Verification

The repository includes scripts that verify behavior against real Windows windows and UI.

```powershell
# Verify placement calculations against real window positions
powershell -ExecutionPolicy Bypass -File tools\verify.ps1

# Trigger and verify global hotkeys with real key input
powershell -ExecutionPolicy Bypass -File tools\verify-hotkeys.ps1

# Verify configuration, gaps, vertical maximize, monitor movement, single-instance behavior, and startup
powershell -ExecutionPolicy Bypass -File tools\verify-more.ps1

# Operate the Settings UI with real mouse/keyboard input and verify persistence
powershell -ExecutionPolicy Bypass -File tools\verify-settings.ps1

# Open the tray menu and verify check marks and toggle behavior
powershell -ExecutionPolicy Bypass -File tools\verify-tray-menu.ps1

# Capture Settings previews for each action
powershell -ExecutionPolicy Bypass -File tools\capture-preview.ps1

# Capture Settings and tray UI
powershell -ExecutionPolicy Bypass -File tools\capture-ui.ps1

# Capture a two-window side-by-side demo
powershell -ExecutionPolicy Bypass -File tools\capture-demo.ps1
```

Installer and release verification:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-installer.ps1
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1
```

Unsigned public releases are published only after install/uninstall smoke tests and SHA-256 verification. Signed releases also require Authenticode validation.

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1 -RequireCodeSigning
```

## Code signing policy

Until SignPath Foundation or another public code-signing provider is connected, EbenTiler may publish verified unsigned GitHub Releases. In that case, the Release page and landing page clearly disclose that the installer is unsigned and that Windows may display an `Unknown publisher` or SmartScreen warning.

Once a code-signing identity is available, subsequent releases will require Authenticode signing and Code Signing EKU verification. Version numbers will always move forward so existing users can detect signed releases as updates instead of replacing an older release in place.

SignPath Foundation is the preferred option currently under review for public OSS signing, but **EbenTiler has not yet been approved by or integrated with SignPath.** Until approval, the project does not claim that SignPath currently signs EbenTiler builds.

See [`docs/CODE_SIGNING.md`](docs/CODE_SIGNING.md) for the detailed policy and provider-selection criteria.

## Notes

- **Windows running as administrator cannot be moved by a lower-privilege EbenTiler process.** To arrange elevated windows, run `EbenTiler.exe` with matching administrator privileges.
- A shortcut may fail to register when another program already owns it. EbenTiler reports failed registrations after startup, and `EbenTiler.exe --check` can be used at any time to inspect them. Change the shortcut in Settings if needed. Game launchers and dock utilities commonly reserve combinations such as `Ctrl+Alt+number` or `Ctrl+Alt+G`.
- Window placement uses the **actual visible frame bounds** reported by DWM, avoiding offsets caused by the transparent resize border around Windows 10/11 windows.
- EbenTiler is per-monitor DPI aware for setups where displays use different scaling factors.

## Project structure

| File | Responsibility |
| --- | --- |
| `src/Native.cs` | Win32 API declarations and DPI-awareness setup |
| `src/SnapAction.cs` | Snap action definitions, display names, default shortcuts |
| `src/Hotkey.cs` | Shortcut parsing and display |
| `src/Config.cs` | Configuration file read/write |
| `src/WindowManager.cs` | Window discovery, placement calculations, movement, restore state |
| `src/HotkeyManager.cs` | Global hotkey registration and dispatch |
| `src/TrayApp.cs` | Tray application, menus, update notifications |
| `src/UpdateChecker.cs` | GitHub Release checks and 24-hour update state |
| `src/SettingsUpdateSection.cs` | Manual update UI under Settings > About |
| `src/SettingsForm.cs` | Settings window |
| `src/Program.cs` | Entry point and command-line mode |
