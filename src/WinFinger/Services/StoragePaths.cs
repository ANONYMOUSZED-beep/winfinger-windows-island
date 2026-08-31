using System.IO;

namespace WinFinger.Services;

/// <summary>Local data layout under %APPDATA%\WinFinger\ (mirrors mac's Application Support/MacFinger).</summary>
public static class StoragePaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinFinger");

    public static string ClipboardMedia { get; } = Path.Combine(Root, "ClipboardMedia");
    public static string ClipboardJson { get; } = Path.Combine(Root, "clipboard.json");
    public static string NotesJson { get; } = Path.Combine(Root, "notes.json");
    public static string SettingsJson { get; } = Path.Combine(Root, "settings.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ClipboardMedia);
    }
}
