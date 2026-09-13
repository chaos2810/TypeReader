# Type Reader

A Windows 11 taskbar widget that instantly displays the last key you press.

- Embedded in the taskbar (WS_CHILD reparenting into Shell_TrayWnd)
- Shows characters as typed, friendly key names, and modifier combos
- Right-click the widget: Settings (font, size, position) / Quit
- Settings persist at %APPDATA%\TypeReader\settings.json
- Stack: WPF .NET 8

## Build

    dotnet build -c Release

## Test

    dotnet test