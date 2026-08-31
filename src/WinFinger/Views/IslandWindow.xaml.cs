using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using WinFinger.Controls;
using WinFinger.Interop;
using WinFinger.ViewModels;

namespace WinFinger.Views;

public partial class IslandWindow : Window
{
    // Island geometry (DIP)
    private const double CompactWidth = 300;
    private const double CompactHeight = 36;
    private const double CompactRadius = 18;
    private const double ExpandedWidth = 720;
    private const double ExpandedHeight = 480;
    private const double ExpandedRadius = 28;

    private const double NotificationWidth = 430;
    private const double HoverWidth = 390;

    private readonly AppViewModel _model;
    private IntPtr _hwnd;
    private IntPtr _mouseHook;
    private NativeMethods.LowLevelMouseProc? _mouseProc; // field: keeps delegate alive against GC
    private readonly System.Windows.Threading.DispatcherTimer _notificationTimer;
    private bool _notificationShowing;
    private bool _hovering;

    public IslandWindow(AppViewModel model)
    {
        _model = model;
        InitializeComponent();
        DataContext = model;
        CompactView.Initialize(model);
        ExpandedView.Initialize(model);

        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) =>
        {
            PositionAtTopCenter();
            _model.ClipboardMonitor.Attach(this);
        };
        PreviewKeyDown += OnPreviewKeyDown;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        model.PropertyChanged += OnModelPropertyChanged;

        _notificationTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(2600)
        };
        _notificationTimer.Tick += (_, _) => HideNotification();
        model.Notifications.NotificationPosted += OnNotificationPosted;
        model.Media.PropertyChanged += OnMediaChangedForGlow;
    }

    // ── Cover-color glow (pulses while music plays) ──

    private void OnMediaChangedForGlow(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Services.MediaService.IsPlaying) or nameof(Services.MediaService.AccentColor))
            UpdateGlow();
    }

    private void UpdateGlow()
    {
        if (_model.Media.IsPlaying)
        {
            IslandShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ColorProperty,
                new ColorAnimation(_model.Media.AccentColor, TimeSpan.FromMilliseconds(600)));
            IslandShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty,
                new DoubleAnimation(0.45, 0.85, TimeSpan.FromMilliseconds(1600))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
        }
        else
        {
            IslandShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ColorProperty,
                new ColorAnimation(System.Windows.Media.Colors.Black, TimeSpan.FromMilliseconds(600)));
            IslandShadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty,
                new DoubleAnimation(0.55, TimeSpan.FromMilliseconds(600)));
        }
    }

    // ── Hover pre-expand (compact state only) ──

    private void OnIslandMouseEnter(object sender, MouseEventArgs e)
    {
        if (_model.IsExpanded || _notificationShowing || _hovering) return;
        _hovering = true;
        AnimateIsland(toWidth: HoverWidth, toHeight: CompactHeight + 6, toRadius: (CompactHeight + 6) / 2,
            duration: TimeSpan.FromMilliseconds(220),
            easing: new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 });
        CompactView.SetHoverState(true);
    }

    private void OnIslandMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_hovering) return;
        _hovering = false;
        CompactView.SetHoverState(false);
        if (_model.IsExpanded || _notificationShowing) return; // another state took over
        AnimateIsland(toWidth: CompactWidth, toHeight: CompactHeight, toRadius: CompactRadius,
            duration: TimeSpan.FromMilliseconds(180),
            easing: new CubicEase { EasingMode = EasingMode.EaseOut });
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        int style = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE,
            style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
    }

    protected override void OnClosed(EventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        RemoveMouseHook();
        base.OnClosed(e);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(PositionAtTopCenter);

    private void PositionAtTopCenter()
    {
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;
    }

    private void OnIslandClicked(object sender, MouseButtonEventArgs e)
    {
        if (!_model.IsExpanded)
            _model.IsExpanded = true;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_model.IsExpanded) return;

        if (e.Key == Key.Escape)
        {
            _model.Collapse();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            AppPage? page = e.Key switch
            {
                Key.D1 => AppPage.Clipboard,
                Key.D2 => AppPage.Media,
                Key.D3 => AppPage.Notes,
                Key.D4 => AppPage.Shortcuts,
                Key.D5 => AppPage.Pomodoro,
                _ => null
            };
            if (page is { } p)
            {
                _model.SelectedPage = p;
                e.Handled = true;
            }
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.IsExpanded))
        {
            if (_model.IsExpanded) Expand();
            else Collapse();
        }
    }

    // ── Expand / collapse choreography ──

    // ── Notification bulge (compact-state only) ──

    private void OnNotificationPosted(Services.IslandNotification notification)
    {
        if (_model.IsExpanded) return;
        if (_hovering)
        {
            _hovering = false;
            CompactView.SetHoverState(false);
        }
        NotificationIcon.Text = notification.Icon;
        NotificationText.Text = notification.Message;
        _notificationTimer.Stop();
        _notificationTimer.Start();
        if (_notificationShowing) return;
        _notificationShowing = true;

        AnimateIsland(toWidth: NotificationWidth, toHeight: CompactHeight, toRadius: CompactRadius,
            duration: TimeSpan.FromMilliseconds(240),
            easing: new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 });

        NotificationView.Visibility = Visibility.Visible;
        FadeTo(CompactView, 0, TimeSpan.FromMilliseconds(80), () => CompactView.Visibility = Visibility.Collapsed);
        NotificationView.Opacity = 0;
        NotificationView.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { BeginTime = TimeSpan.FromMilliseconds(120) });
    }

    private void HideNotification()
    {
        _notificationTimer.Stop();
        if (!_notificationShowing) return;
        _notificationShowing = false;
        if (_model.IsExpanded) return; // expand animation already took over

        AnimateIsland(toWidth: CompactWidth, toHeight: CompactHeight, toRadius: CompactRadius,
            duration: TimeSpan.FromMilliseconds(200),
            easing: new CubicEase { EasingMode = EasingMode.EaseInOut });

        CompactView.Visibility = Visibility.Visible;
        FadeTo(NotificationView, 0, TimeSpan.FromMilliseconds(80), () => NotificationView.Visibility = Visibility.Collapsed);
        CompactView.Opacity = 0;
        CompactView.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { BeginTime = TimeSpan.FromMilliseconds(100) });
    }

    private void Expand()
    {
        if (_hovering)
        {
            _hovering = false;
            CompactView.SetHoverState(false);
        }
        if (_notificationShowing)
        {
            _notificationTimer.Stop();
            _notificationShowing = false;
            NotificationView.Visibility = Visibility.Collapsed;
            NotificationView.Opacity = 0;
        }
        SetNoActivate(false);
        Activate();
        Focus();

        AnimateIsland(toWidth: ExpandedWidth, toHeight: ExpandedHeight, toRadius: ExpandedRadius,
            duration: TimeSpan.FromMilliseconds(280),
            easing: new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.32 });

        // Content crossfade: compact out fast, expanded in after ~70% of the resize.
        ExpandedView.Visibility = Visibility.Visible;
        FadeTo(CompactView, 0, TimeSpan.FromMilliseconds(90), () => CompactView.Visibility = Visibility.Collapsed);
        ExpandedView.Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
        {
            BeginTime = TimeSpan.FromMilliseconds(190),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ExpandedView.BeginAnimation(OpacityProperty, fadeIn);

        InstallMouseHook();
    }

    private void Collapse()
    {
        RemoveMouseHook();
        SetNoActivate(true);

        AnimateIsland(toWidth: CompactWidth, toHeight: CompactHeight, toRadius: CompactRadius,
            duration: TimeSpan.FromMilliseconds(180),
            easing: new CubicEase { EasingMode = EasingMode.EaseIn });

        CompactView.Visibility = Visibility.Visible;
        FadeTo(ExpandedView, 0, TimeSpan.FromMilliseconds(90), () => ExpandedView.Visibility = Visibility.Collapsed);
        CompactView.Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
        {
            BeginTime = TimeSpan.FromMilliseconds(120),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        CompactView.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void AnimateIsland(double toWidth, double toHeight, double toRadius, TimeSpan duration, IEasingFunction easing)
    {
        var widthAnim = new DoubleAnimation(toWidth, duration) { EasingFunction = easing };
        var heightAnim = new DoubleAnimation(toHeight, duration) { EasingFunction = easing };
        var radiusAnim = new CornerRadiusAnimation
        {
            From = IslandBorder.CornerRadius,
            To = new CornerRadius(toRadius),
            Duration = duration,
            EasingFunction = easing
        };
        IslandBorder.BeginAnimation(WidthProperty, widthAnim);
        IslandBorder.BeginAnimation(HeightProperty, heightAnim);
        IslandBorder.BeginAnimation(System.Windows.Controls.Border.CornerRadiusProperty, radiusAnim);
    }

    private static void FadeTo(UIElement element, double to, TimeSpan duration, Action? completed = null)
    {
        var anim = new DoubleAnimation(to, duration);
        if (completed is not null)
            anim.Completed += (_, _) => completed();
        element.BeginAnimation(OpacityProperty, anim);
    }

    private void SetNoActivate(bool enabled)
    {
        if (_hwnd == IntPtr.Zero) return;
        int style = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
        style = enabled ? style | NativeMethods.WS_EX_NOACTIVATE : style & ~NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, style);
    }

    // ── Click-outside detection (low-level mouse hook, installed only while expanded) ──

    private void InstallMouseHook()
    {
        if (_mouseHook != IntPtr.Zero) return;
        _mouseProc = MouseHookCallback;
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc,
            NativeMethods.GetModuleHandle(null), 0);
    }

    private void RemoveMouseHook()
    {
        if (_mouseHook == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_mouseHook);
        _mouseHook = IntPtr.Zero;
        _mouseProc = null;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Zero blocking work here: capture the point, decide on the dispatcher.
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg is NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_RBUTTONDOWN)
            {
                var data = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                var screenPoint = new Point(data.pt.X, data.pt.Y);
                Dispatcher.BeginInvoke(() =>
                {
                    if (!_model.IsExpanded) return;
                    // PointToScreen yields device pixels — same space as the hook's point.
                    var topLeft = IslandBorder.PointToScreen(new Point(0, 0));
                    var bottomRight = IslandBorder.PointToScreen(new Point(IslandBorder.ActualWidth, IslandBorder.ActualHeight));
                    var bounds = new Rect(topLeft, bottomRight);
                    if (!bounds.Contains(screenPoint))
                        _model.Collapse();
                });
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }
}
