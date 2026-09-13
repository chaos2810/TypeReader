# Type Reader

A small widget that lives in your Windows taskbar and instantly shows every key you press — the moment you press it.

Type Reader is for anyone who wants to see their keystrokes at a glance: while typing, testing a keyboard, following a tutorial, or showing shortcuts to someone nearby. It sits quietly on the taskbar, and the latest key is always visible.

## Getting started

1. Run `TypeReader.App.exe` (or see *Build from source* below).
2. The widget appears in your taskbar right away — no window, no setup.
3. Press any key. That's it.

A small keyboard icon is added to the system tray (near the clock) so you can always reach the widget's settings or quit it.

## Reading the display

The widget always shows your **most recent** key. Each new press replaces the previous display instantly.

| You press | Widget shows |
|---|---|
| `a` | a |
| `Shift` + `A` | A |
| `1` (with Shift) | ! |
| `Ctrl` + `S` | Ctrl + S |
| `Ctrl` + `Shift` + `T` | Ctrl + Shift + T |
| `F5` | F5 |
| `Enter`, `Esc`, `Space` | Enter, Esc, Space |
| Arrow keys | ← ↑ → ↓ |
| `Win` (alone) | Win |

Details worth knowing:

- Characters appear exactly as typed, following your **actual keyboard layout** — any language works, including Cyrillic, Greek, and CJK.
- With Caps Lock on, letters appear as your keyboard produces them (`A` for `a`).
- Holding modifiers shows the combination once you press another key (`Ctrl + S`). A modifier pressed on its own just shows its name.
- A `…` placeholder is shown until the first key is pressed.

## The right-click menu

Right-click anywhere on the widget to open its menu:

- **Settings** — open the settings window.
- **Quit** — close the widget and remove it from the taskbar.

The tray icon has the same two options, so you can always reach them even if the widget itself is hard to click.

## Settings

**Size** — pick the text size (10–24). The preview updates as you choose.

**Position** — choose where the widget sits: **Left**, **Center**, or **Right**. Type Reader automatically slides into free space: it never covers pinned apps, the clock, system tray icons, or other taskbar widgets you may be running. If a neighbor moves or appears, the widget quietly adjusts itself.

Changes apply immediately and are remembered. They are saved to:

    %APPDATA%\TypeReader\settings.json

If that file is missing or unreadable, the widget simply starts with its defaults.

## Look and feel

Type Reader is designed to look native on Windows 11:

- A frosted, rounded box that matches the taskbar's acrylic material.
- Light and dark themes follow your Windows setting automatically.
- Hovering the widget gently lightens it, like native taskbar buttons.

## Reliability

- If Explorer restarts (or crashes), the widget reappears on its own once the taskbar comes back.
- Starting the app a second time does nothing — one instance is enough.

## Privacy

Keystrokes are shown on your screen and nowhere else. Type Reader does not log, store, or send anything you type. The only file it ever writes is its own settings file.

## Build from source

Requires the .NET 8 SDK:

    dotnet build -c Release
    dotnet run --project src\TypeReader.App -c Release

## Troubleshooting

**The widget isn't showing.** It embeds itself once the taskbar is ready. If it never appears, click the tray icon → Settings and try each position.

**I want to reset everything.** Close the app and delete `%APPDATA%\TypeReader\settings.json`, then start it again.

**The widget overlaps something.** Press a key or wait a second — it re-checks free space every second and a half. You can also pick a different position in Settings.