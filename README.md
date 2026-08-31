# WinFinger

A Dynamic Island productivity companion for Windows—a Windows port of the macOS notch utility [MacFinger], enhanced with an iOS-inspired Dynamic Island experience.

A liquid-glass capsule stays at the top of your screen and shows live network and memory usage. Click it to smoothly expand into a five-page panel for **clipboard history, media controls, notes, shortcuts, and a Pomodoro timer**.

## Download

Download the latest `WinFinger.exe` from [Releases](../../releases). It is a self-contained single-file app, so no .NET installation is required—just double-click to run it.

## Features

| Module | Description |
|---|---|
| Compact Island | A centered black capsule with album art on the left while media is playing and live `↓ download ↑ upload · memory %` metrics on the right. |
| Clipboard History | Event-driven `WM_CLIPBOARDUPDATE` monitoring for text and images, SHA-256 deduplication, a 100-item limit, PNG image storage, pause/clear/restore actions, and source-app tracking. |
| Media Controls | System-wide GSMTC media sessions with artwork, title, artist, play/pause, previous, and next controls. Supports Spotify, browsers, NetEase Cloud Music, and more. |
| Notes | Note list and editor with 500 ms debounced autosave, pinned-note sorting, and `Ctrl+N` to create a note. |
| Shortcut Guide | Automatically switches shortcuts for the foreground app: File Explorer, Chrome, Edge, VS Code, Word, Excel, WeChat, and Windows Terminal. Falls back to general Windows shortcuts. |
| Pomodoro Timer | Configurable focus/break cycles, a compact-island countdown, sound, and in-island completion notifications. |
| Island Notifications | Clipboard captures and Pomodoro events briefly expand the capsule into a three-second notification banner. |
| Audio Visualizer | Eight live spectrum bars powered by WASAPI loopback and FFT while music is playing. |
| Artwork Glow | Extracts the dominant album-art color and uses it for a softly pulsing glow around the island. |
| Hover Preview | Gently enlarges the capsule and reveals the current track on hover; clicking fully expands it. |
| Liquid Glass | Custom real-time frosted glass captures the screen behind the island, downsamples and blurs it, boosts saturation, and renders it beneath the UI. It also includes a refractive rim, moving edge highlights, chromatic bevels, a curved top reflection, an expansion sheen, and album-color tinting. |
| Ghost Mode | Fades the island to 40% opacity and enables click-through when the pointer is far away, preventing it from blocking browser tabs. It becomes interactive again when approached. |
| Drag to Reposition | Drag the capsule horizontally; its position is preserved after restart. |

## Controls

- **Click the capsule** to expand it; press **Esc** or **click outside the panel** to collapse it.
- Press **Ctrl+1..5** to switch between Clipboard, Media, Notes, Shortcuts, and Pomodoro.
- The tray menu provides Open, Pause Clipboard Capture, Clear Clipboard History, Start with Windows, and Quit actions.
- WinFinger has no taskbar button and does not appear in Alt+Tab.

## Typography

The interface uses **Segoe UI Variable Display** for headings and **Segoe UI Variable Text** for controls and body copy, with standard Segoe UI fallbacks. This native Windows type family keeps the liquid-glass interface clean and modern without bundling an extra font.

## Technology

WPF / .NET 8 (`net8.0-windows10.0.19041.0`) with built-in CsWinRT projections for WinRT media APIs, CommunityToolkit.Mvvm, Hardcodet.NotifyIcon.Wpf, and Per-Monitor V2 DPI support.

The window is a fixed-size, transparent, borderless, always-on-top stage. Only the inner border's width, height, and corner radius are animated: a 280 ms elastic `BackEase` expansion and a 180 ms `CubicEase` collapse. Transparent pixels naturally pass clicks through.

Windows acrylic (`DWM SystemBackdrop`) desaturates the background too heavily for the intended colorful liquid-glass effect, so WinFinger uses a custom implementation. At 12.5 fps, GDI `StretchBlt` downsamples the screen area behind the island into a 128×84 DIB, applies two box-blur passes, 1.6× saturation, and a brightness lift, then writes the result to a `WriteableBitmap` used as the island's base `ImageBrush`. The island window uses `WDA_EXCLUDEFROMCAPTURE` to prevent recursive capture feedback.

> Because of `WDA_EXCLUDEFROMCAPTURE`, **the island does not appear in screenshots, screen recordings, or screen sharing**, similar to DRM-protected windows. Use a camera if you need an image that includes the island.

> Known limitation: WinFinger does not intercept system toast notifications. WinRT `UserNotificationListener` requires MSIX package identity and is unavailable to an unpackaged executable. In-island notifications are limited to WinFinger's own events.

## Build

Requires the .NET 8 SDK:

```bash
dotnet build
dotnet run --project src/WinFinger
```

Publish a self-contained single-file build (approximately 75 MB):

```bash
dotnet publish src/WinFinger/WinFinger.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

Output: `src/WinFinger/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/WinFinger.exe`

## Local Data

```text
%APPDATA%\WinFinger\
├── clipboard.json      # Clipboard metadata, compatible with the macOS version
├── notes.json          # Notes
├── settings.json       # Settings
└── ClipboardMedia\     # Clipboard images as PNG files
```

## Requirements and Privacy

- Windows 10 version 1809 or later; Windows 11 is recommended.
- Clipboard history may contain sensitive information such as passwords. Entries are stored unencrypted on your local disk. You can pause capture or clear the history at any time.

## License

For learning and personal use only.
