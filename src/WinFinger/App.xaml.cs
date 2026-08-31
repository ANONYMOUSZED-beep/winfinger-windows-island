using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using WinFinger.ViewModels;
using WinFinger.Views;

namespace WinFinger;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private TaskbarIcon? _trayIcon;
    private IslandWindow? _islandWindow;

    public AppViewModel Model { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, @"Global\WinFinger.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Model.Start();

        _islandWindow = new IslandWindow(Model);
        _islandWindow.Show();

        CreateTrayIcon();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        if (_ownsMutex)
        {
            Model.Stop();
            _singleInstanceMutex?.ReleaseMutex();
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        var menu = new System.Windows.Controls.ContextMenu
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI")
        };

        var openItem = new System.Windows.Controls.MenuItem { Header = "Open WinFinger" };
        openItem.Click += (_, _) => Model.IsExpanded = true;
        menu.Items.Add(openItem);

        var pauseItem = new System.Windows.Controls.MenuItem
        {
            Header = "Pause clipboard capture",
            IsCheckable = true,
            IsChecked = Model.ClipboardMonitor.IsPaused
        };
        pauseItem.Click += (_, _) => Model.ClipboardMonitor.IsPaused = pauseItem.IsChecked;
        menu.Items.Add(pauseItem);

        var clearItem = new System.Windows.Controls.MenuItem { Header = "Clear clipboard history" };
        clearItem.Click += (_, _) => Model.ClipboardStore.Clear();
        menu.Items.Add(clearItem);

        var glassItem = new System.Windows.Controls.MenuItem
        {
            Header = "Live glass background (uses more resources)",
            IsCheckable = true,
            IsChecked = Model.SettingsStore.Settings.LiveGlassEnabled
        };
        glassItem.Click += (_, _) =>
        {
            Model.SettingsStore.Settings.LiveGlassEnabled = glassItem.IsChecked;
            Model.SettingsStore.Save();
            _islandWindow?.SetLiveGlass(glassItem.IsChecked);
        };
        menu.Items.Add(glassItem);

        var autoStartItem = new System.Windows.Controls.MenuItem
        {
            Header = "Start with Windows",
            IsCheckable = true,
            IsChecked = Model.SettingsStore.Settings.AutoStart
        };
        autoStartItem.Click += (_, _) => Model.SettingsStore.SetAutoStart(autoStartItem.IsChecked);
        menu.Items.Add(autoStartItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var quitItem = new System.Windows.Controls.MenuItem { Header = "Quit WinFinger" };
        quitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(quitItem);

        _trayIcon = new TaskbarIcon
        {
            Icon = CreatePillIcon(),
            ToolTipText = "WinFinger",
            ContextMenu = menu
        };
        _trayIcon.TrayLeftMouseUp += (_, _) => Model.ToggleExpanded();
    }

    /// <summary>Draws the island pill as a 32x32 tray icon at runtime (no .ico asset needed).</summary>
    private static Icon CreatePillIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            var rect = new Rectangle(2, 10, 28, 12);
            int r = rect.Height;
            path.AddArc(rect.X, rect.Y, r, r, 90, 180);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 180);
            path.CloseFigure();
            using var fill = new SolidBrush(System.Drawing.Color.FromArgb(255, 20, 20, 22));
            using var stroke = new Pen(System.Drawing.Color.FromArgb(200, 235, 235, 240), 1.6f);
            g.FillPath(fill, path);
            g.DrawPath(stroke, path);
        }
        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
