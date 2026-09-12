# Type Reader — Taskbar Key Display Widget

**Date:** 2026-09-12
**Status:** Approved design (brainstorming complete)

## Overview

Type Reader is a Windows 11 utility that embeds a widget inside the taskbar and instantly displays every key the user presses — letters, numbers, symbols, function keys, and modifier combos (e.g. `Ctrl + S`). It is a personal typing monitor: display is the only consumer of keystrokes; nothing is logged or transmitted.

Core requirements:

- Widget lives inside the taskbar (visually part of it), Windows 11 Pro 25H2.
- Shows the **last key only** — each keypress instantly replaces the previous display.
- Letter keys show the character as typed (`a`, `A`); special keys show friendly names (`F1`, `Enter`, `Esc`, `Left Shift`).
- Modifier combos display as `X + Y` (e.g. `Ctrl + S`); Shift+A displays `A` (the shifted character).
- Right-click on the widget opens a context menu: **Settings** and **Quit**.
- Settings include a font picker (installed system fonts) and font size; also widget position.
- User-selectable taskbar position: **Left / Center / Right**.

## Tech stack and phase-gate

### Phase 1 — WinUI 3 prototype (time-boxed)

Minimal WinUI 3 app that embeds a borderless text widget into the taskbar using the FluentFlyout technique: obtain the window HWND (via `IWindowNative`), strip `WS_POPUP`, add `WS_CHILD` via `SetWindowLong`, then `SetParent` into `Shell_TrayWnd`.

**Success criteria** (all must pass to continue with WinUI 3):

1. Widget renders text on the taskbar.
2. Widget receives right-click input and shows a context menu.
3. No crash when Explorer restarts (taskbar handle invalidated and restored).
4. No rendering artifacts (blink, wrong z-order, input stealing from the taskbar).

**Documented risks** (microsoft-ui-xaml issues): #8707/#8779 (child window not resizing after `SetParent` on Win11), #9203 (exception on `Close()` with parented windows), #10259 (content blink during load). WinUI 3's AppWindow assumes a top-level HWND; reparenting is unsupported and purely a P/Invoke hack.

### Phase 2 — Fallback: WPF on .NET 8

If any success criterion fails, fall back to WPF .NET 8 and replicate FluentFlyout's proven `TaskbarWindow` approach (studied in `FluentFlyoutWPF/Windows/TaskbarWindow.xaml.cs`): `WS_CHILD` + `SetParent` into `Shell_TrayWnd`, `SetWindowPos` positioning, `SetWindowRgn` clipping, periodic re-parent check timer.

The prototype is throwaway; the production code structure is the same for both stacks.

## Architecture

Single project, small focused units:

```
TypeReader/
├── App.xaml(.cs)              — entry point, single-instance guard, tray icon
├── KeyboardHook.cs            — WH_KEYBOARD_LL wrapper → KeyPressed event (VK code + modifiers)
├── KeyFormatter.cs            — VK → display string ("a", "A", "F1", "Ctrl + S", "Esc")
├── NativeMethods.cs           — P/Invoke: SetParent, SetWindowLong, FindWindow, etc.
├── TaskbarEmbedder.cs         — WS_CHILD + SetParent into Shell_TrayWnd, position & re-parent logic
├── WidgetWindow.xaml(.cs)     — the embedded widget UI: text + right-click menu
└── SettingsWindow.xaml(.cs)   — font picker, size, position
```

Each unit has one purpose and a clear interface:

- **KeyboardHook** — what: captures global keypresses; use: subscribe to `KeyPressed` event; depends: nothing (pure capture, no formatting).
- **KeyFormatter** — what: maps VK codes + modifier state to display strings; use: `string Format(VK, modifiers, char)`; depends: nothing. Pure and unit-testable.
- **TaskbarEmbedder** — what: embeds and positions the widget window in the taskbar; use: `Embed(HWND widget)`, `UpdatePosition()`, exposes `TaskbarHandle`; depends: NativeMethods.
- **WidgetWindow** — what: displays the formatted key text, hosts context menu; depends: KeyboardHook (event), KeyFormatter, TaskbarEmbedder.
- **SettingsWindow** — what: edits settings; depends: shared settings store.

## Keyboard capture

- `SetWindowsHookEx(WH_KEYBOARD_LL)` low-level keyboard hook on the app's own thread.
- Global, instantaneous, no admin rights required.
- The hook callback forwards VK code + modifier state to subscribers and calls through (never swallows) so typing works normally.
- No logging, no persistence of keystrokes, no network.

## Key display rules

| Input | Display |
|---|---|
| Character key (letter/number/symbol) | The character as typed: `a`, `A`, `!` (via current keyboard layout, `ToUnicodeEx`) |
| Function keys | `F1` … `F12` |
| Named keys | `Enter`, `Esc`, `Space`, `Tab`, `Backspace`, `Delete`, `Home`, `End`, `Page Up`, `Page Down`, arrows (`↑ ↓ ← →`), `Print Screen`, etc. |
| Modifier pressed alone | `Ctrl`, `Shift`, `Alt`, `Win` (shown until next key) |
| Modifier combo | `Ctrl + S`, `Ctrl + Shift + T` — modifiers accumulated while held, flushed as combo when a non-modifier key is pressed |
| Shift + letter | `A` (the shifted character, not "Shift + A") |

- Combo accumulation: modifier keydowns build a set; if a non-modifier fires, display `mods + key`; if modifiers are released without a combo, display the last modifier pressed.
- Only the **last key/combo** is displayed — no history, no accumulation strip.

## Positioning

- Position enum: `Left`, `Center`, `Right` — user-selectable, persisted.
- Left: near the Start/widgets area. Center: middle of the taskbar alongside pinned icons. Right: near the system tray (`TrayNotifyWnd` bounds used as anchor).
- Centered vertically within the taskbar's height; DPI-aware (`GetDpiForWindow`).
- Widget width auto-fits the longest display string in the chosen font; window clipped to the widget rect.
- Primary taskbar only (single `Shell_TrayWnd`); secondary taskbars out of scope.

## Recovery & lifecycle

- Startup: if `Shell_TrayWnd` is not found (Explorer still starting), retry every 1 second (up to 30 attempts), then show tray notification with a retry action.
- Periodic check (e.g. every 1.5s, mirroring FluentFlyout): if `GetParent(widget)` ≠ current `Shell_TrayWnd` handle, re-embed and re-position (covers Explorer crash/restart).
- Tray icon (system notification area) provides an always-accessible way to open Settings/Quit even if the widget is unreachable.

## Settings & persistence

- JSON file at `%APPDATA%\TypeReader\settings.json`.
- Fields: `FontFamily` (string), `FontSize` (double), `Position` (Left|Center|Right).
- Font list from `InstalledFontCollection` (system fonts), live preview in the settings window.
- Defaults: `Segoe UI`, 14pt, Center.
- Corrupt/unreadable settings file → fall back to defaults silently.
- Settings changes apply immediately (live update of the widget).

## Error handling

| Scenario | Behavior |
|---|---|
| Taskbar not found at startup | Retry loop (Explorer may still be starting); widget hidden until embedded |
| Explorer crash/restart | Auto re-embed via periodic parent check |
| Keyboard hook installation fails | Tray notification warning; app continues without capture |
| Settings file corrupt | Silent defaults |
| Font name in settings no longer installed | Fall back to default `Segoe UI` |

## Testing

- **Unit tests**: `KeyFormatter` — VK→string mapping, shifted characters, combo accumulation, combo flush on non-modifier, modifier-only display, edge cases (dead keys, `ToUnicodeEx` failures).
- **Manual test checklist** (prototype gate and release): embed + render, right-click menu, Explorer restart recovery, font change live-applied, position change, single-instance guard, quit from menu.
- The Phase 1 prototype **is** the integration feasibility test for the WinUI 3 stack decision.

## Out of scope

- Key history / accumulating display
- Secondary taskbars / multi-monitor
- Custom font file loading (.ttf/.otf), font colors, bold/italic toggles
- Logging keystrokes anywhere
- Auto-start with Windows
- MSIX packaging (plain executable first)