using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WinFinger.Models;

namespace WinFinger.Services;

/// <summary>Clipboard history persistence (mirrors mac ClipboardStore: dedupe, cap, PNG on disk).</summary>
public sealed class ClipboardStore
{
    public const int MaxEntries = 100;
    public const int MaxImageBytes = 10 * 1024 * 1024;

    public ObservableCollection<ClipboardEntry> Entries { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ClipboardStore()
    {
        Load();
    }

    public void AppendText(string text, string? sourceApp)
    {
        if (string.IsNullOrEmpty(text)) return;
        var hash = Hash(Encoding.UTF8.GetBytes(text));
        if (Entries.Any(e => e.ContentHash == hash)) return;

        Entries.Insert(0, new ClipboardEntry(Guid.NewGuid(), ClipboardEntryKind.Text, text,
            null, null, sourceApp, DateTime.Now, hash));
        TrimAndSave();
    }

    public void AppendImage(byte[] pngData, string? sourceApp)
    {
        if (pngData.Length == 0 || pngData.Length > MaxImageBytes) return;
        var hash = Hash(pngData);
        if (Entries.Any(e => e.ContentHash == hash)) return;

        var id = Guid.NewGuid();
        var path = Path.Combine(StoragePaths.ClipboardMedia, $"{id}.png");
        try
        {
            File.WriteAllBytes(path, pngData);
        }
        catch
        {
            return;
        }

        Entries.Insert(0, new ClipboardEntry(id, ClipboardEntryKind.Image, null,
            path, null, sourceApp, DateTime.Now, hash));
        TrimAndSave();
    }

    public void Remove(ClipboardEntry entry)
    {
        Entries.Remove(entry);
        DeleteImageFile(entry);
        Save();
    }

    public void Clear()
    {
        foreach (var entry in Entries)
            DeleteImageFile(entry);
        Entries.Clear();
        Save();
    }

    public byte[]? ImageData(ClipboardEntry entry)
    {
        if (entry.ImagePath is null) return null;
        try
        {
            return File.ReadAllBytes(entry.ImagePath);
        }
        catch
        {
            return null;
        }
    }

    private static void DeleteImageFile(ClipboardEntry entry)
    {
        if (entry.ImagePath is null) return;
        try
        {
            File.Delete(entry.ImagePath);
        }
        catch
        {
            // best effort
        }
    }

    private void TrimAndSave()
    {
        while (Entries.Count > MaxEntries)
        {
            var last = Entries[^1];
            Entries.RemoveAt(Entries.Count - 1);
            DeleteImageFile(last);
        }
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(StoragePaths.ClipboardJson)) return;
            var decoded = JsonSerializer.Deserialize<List<ClipboardEntry>>(
                File.ReadAllText(StoragePaths.ClipboardJson), JsonOptions);
            if (decoded is null) return;
            foreach (var entry in decoded)
            {
                if (entry.Kind == ClipboardEntryKind.Image &&
                    (entry.ImagePath is null || !File.Exists(entry.ImagePath)))
                    continue;
                Entries.Add(entry);
            }
        }
        catch
        {
            // corrupt file: start fresh
        }
    }

    private void Save()
    {
        try
        {
            StoragePaths.EnsureCreated();
            File.WriteAllText(StoragePaths.ClipboardJson,
                JsonSerializer.Serialize(Entries.ToList(), JsonOptions));
        }
        catch
        {
            // best effort
        }
    }

    public static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
