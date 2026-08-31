using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WinFinger.Services;
using WinFinger.ViewModels;

namespace WinFinger.Views;

public partial class CompactIslandView : UserControl
{
    private AppViewModel? _model;
    private Rectangle[] _bars = Array.Empty<Rectangle>();

    public CompactIslandView()
    {
        InitializeComponent();
    }

    public void Initialize(AppViewModel model)
    {
        _model = model;

        DownloadLabel.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(MetricsService.DownloadText)) { Source = model.Metrics });
        UploadLabel.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(MetricsService.UploadText)) { Source = model.Metrics });
        MemoryLabel.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(MetricsService.MemoryText)) { Source = model.Metrics });

        _bars = SpectrumPanel.Children.OfType<Rectangle>().ToArray();
        model.Visualizer.LevelsUpdated += OnLevelsUpdated;

        model.Media.PropertyChanged += OnMediaChanged;
        model.Pomodoro.PropertyChanged += OnPomodoroChanged;
        RefreshMedia();
        RefreshPomodoro();
    }

    /// <summary>Reveals/hides the now-playing title during hover pre-expand.</summary>
    public void SetHoverState(bool hovering)
    {
        if (_model is null) return;
        bool show = hovering && _model.Media.HasSession && _model.Media.Title.Length > 0
                    && PomodoroLabel.Visibility != Visibility.Visible;
        if (show)
        {
            HoverTitleLabel.Text = _model.Media.Title;
            HoverTitleLabel.Visibility = Visibility.Visible;
            HoverTitleLabel.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(160)) { BeginTime = TimeSpan.FromMilliseconds(80) });
        }
        else
        {
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(100));
            fade.Completed += (_, _) => HoverTitleLabel.Visibility = Visibility.Collapsed;
            HoverTitleLabel.BeginAnimation(OpacityProperty, fade);
        }
    }

    private void OnLevelsUpdated()
    {
        var visualizer = _model?.Visualizer;
        if (visualizer is null) return;
        bool running = visualizer.IsRunning;
        SpectrumPanel.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        if (!running) return;
        var brush = SpectrumBrush();
        for (int i = 0; i < _bars.Length && i < AudioVisualizerService.BandCount; i++)
        {
            _bars[i].Height = 3 + visualizer.Levels[i] * 13;
            _bars[i].Fill = brush;
        }
    }

    private Brush SpectrumBrush()
    {
        var color = _model?.Media.AccentColor ?? Colors.White;
        // keep bars readable even for dark accents
        var brush = new SolidColorBrush(Color.FromRgb(
            (byte)Math.Min(255, color.R + 40),
            (byte)Math.Min(255, color.G + 40),
            (byte)Math.Min(255, color.B + 40)));
        brush.Freeze();
        return brush;
    }

    private void OnMediaChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MediaService.Cover) or nameof(MediaService.HasSession) or nameof(MediaService.IsPlaying))
            RefreshMedia();
    }

    private void RefreshMedia()
    {
        if (_model is null) return;
        var media = _model.Media;
        bool showCover = media.HasSession && media.Cover is not null;
        CoverSlot.Visibility = showCover ? Visibility.Visible : Visibility.Collapsed;
        IdleDot.Visibility = showCover ? Visibility.Collapsed : Visibility.Visible;
        if (showCover) CoverImage.Source = media.Cover;
    }

    private void OnPomodoroChanged(object? sender, PropertyChangedEventArgs e) => RefreshPomodoro();

    private void RefreshPomodoro()
    {
        if (_model is null) return;
        var pomodoro = _model.Pomodoro;
        if (pomodoro.Phase == PomodoroPhase.Idle)
        {
            PomodoroLabel.Visibility = Visibility.Collapsed;
            return;
        }
        PomodoroLabel.Visibility = Visibility.Visible;
        PomodoroLabel.Text = $"🍅 {pomodoro.RemainingText}";
    }
}
