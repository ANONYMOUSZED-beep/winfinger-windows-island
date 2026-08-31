using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using WinFinger.Interop;
using WinFinger.Models;

namespace WinFinger.Services;

/// <summary>Listens for WM_CLIPBOARDUPDATE and records history into the store.</summary>
public sealed partial class ClipboardMonitorService : ObservableObject
{
    [ObservableProperty] private bool _isPaused;

    private readonly ClipboardStore _store;
    private HwndSource? _source;
    private IntPtr _hwnd;
    private string? _ignoreHash;      // content we just wrote back ourselves
    private DateTime _suppressUntil;  // belt-and-braces time window

    public ClipboardMonitorService(ClipboardStore store)
    {
        _store = store;
    }

    /// <summary>Attach the listener to an existing window's message loop.</summary>
    public void Attach(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
        NativeMethods.AddClipboardFormatListener(_hwnd);
    }

    public void Detach()
    {
        if (_hwnd != IntPtr.Zero)
            NativeMethods.RemoveClipboardFormatListener(_hwnd);
        _source?.RemoveHook(WndProc);
        _source = null;
        _hwnd = IntPtr.Zero;
    }

    /// <summary>Writes an entry back to the system clipboard without re-recording it.</summary>
    public void CopyToClipboard(ClipboardEntry entry)
    {
        try
        {
            if (entry.Kind == ClipboardEntryKind.Text && entry.Text is { } text)
            {
                _ignoreHash = ClipboardStore.Hash(Encoding.UTF8.GetBytes(text));
                _suppressUntil = DateTime.UtcNow.AddMilliseconds(500);
                WithRetry(() => Clipboard.SetText(text));
            }
            else if (entry.Kind == ClipboardEntryKind.Image && _store.ImageData(entry) is { } png)
            {
                _ignoreHash = ClipboardStore.Hash(png);
                _suppressUntil = DateTime.UtcNow.AddMilliseconds(500);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = new MemoryStream(png);
                image.EndInit();
                image.Freeze();
                WithRetry(() => Clipboard.SetImage(image));
            }
        }
        catch
        {
            // clipboard is contended; give up silently
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            OnClipboardChanged();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        if (IsPaused) return;

        var sourceApp = ForegroundProcessName();
        if (string.Equals(sourceApp, Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase))
            sourceApp = null;

        try
        {
            string? text = null;
            WithRetry(() =>
            {
                if (Clipboard.ContainsText()) text = Clipboard.GetText();
            });
            if (!string.IsNullOrEmpty(text))
            {
                var hash = ClipboardStore.Hash(Encoding.UTF8.GetBytes(text));
                if (ShouldIgnore(hash)) return;
                _store.AppendText(text, sourceApp);
                return;
            }

            BitmapSource? image = null;
            WithRetry(() =>
            {
                if (Clipboard.ContainsImage()) image = Clipboard.GetImage();
            });
            if (image is not null)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                var png = stream.ToArray();
                var hash = ClipboardStore.Hash(png);
                if (ShouldIgnore(hash)) return;
                _store.AppendImage(png, sourceApp);
            }
        }
        catch
        {
            // reading can race with the copying app; skip this update
        }
    }

    private bool ShouldIgnore(string hash)
    {
        if (_ignoreHash == hash && DateTime.UtcNow <= _suppressUntil)
        {
            _ignoreHash = null;
            return true;
        }
        _ignoreHash = null;
        return false;
    }

    private static string? ForegroundProcessName()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Clipboard access races with other apps (CLIPBRD_E_CANT_OPEN); retry 3×50ms.</summary>
    private static void WithRetry(Action action)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 2)
            {
                Thread.Sleep(50);
            }
        }
    }
}
