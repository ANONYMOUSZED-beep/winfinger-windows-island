using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinFinger.ViewModels;
using WinFinger.Views.Pages;

namespace WinFinger.Views;

public partial class ExpandedPanelView : UserControl
{
    private AppViewModel? _model;
    private readonly Dictionary<AppPage, UserControl> _pages = new();
    private bool _syncing;

    public ExpandedPanelView()
    {
        InitializeComponent();
    }

    public void Initialize(AppViewModel model)
    {
        _model = model;

        _pages[AppPage.Clipboard] = new ClipboardPage();
        _pages[AppPage.Media] = new MediaPage();
        _pages[AppPage.Notes] = new NotesPage();
        _pages[AppPage.Shortcuts] = new ShortcutsPage();
        _pages[AppPage.Pomodoro] = new PomodoroPage();
        foreach (var page in _pages.Values)
            (page as IIslandPage)?.Initialize(model);

        WireTab(TabClipboard, AppPage.Clipboard);
        WireTab(TabMedia, AppPage.Media);
        WireTab(TabNotes, AppPage.Notes);
        WireTab(TabShortcuts, AppPage.Shortcuts);
        WireTab(TabPomodoro, AppPage.Pomodoro);
        CloseButton.Click += (_, _) => model.Collapse();

        model.PropertyChanged += OnModelPropertyChanged;
        SyncFromModel(animated: false);
    }

    private void WireTab(RadioButton tab, AppPage page)
    {
        tab.Checked += (_, _) =>
        {
            if (_syncing || _model is null) return;
            _model.SelectedPage = page;
        };
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.SelectedPage))
            SyncFromModel(animated: true);
    }

    private void SyncFromModel(bool animated)
    {
        if (_model is null) return;
        _syncing = true;
        try
        {
            TabClipboard.IsChecked = _model.SelectedPage == AppPage.Clipboard;
            TabMedia.IsChecked = _model.SelectedPage == AppPage.Media;
            TabNotes.IsChecked = _model.SelectedPage == AppPage.Notes;
            TabShortcuts.IsChecked = _model.SelectedPage == AppPage.Shortcuts;
            TabPomodoro.IsChecked = _model.SelectedPage == AppPage.Pomodoro;
        }
        finally
        {
            _syncing = false;
        }

        var page = _pages[_model.SelectedPage];
        if (ReferenceEquals(PageHost.Content, page)) return;
        PageHost.Content = page;
        (page as IIslandPage)?.OnShown();

        if (animated)
        {
            // Fade + slide-up entrance
            page.Opacity = 0;
            var translate = new TranslateTransform(0, 14);
            page.RenderTransform = translate;
            page.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }
}

/// <summary>Implemented by expanded-panel pages to receive the shared model.</summary>
public interface IIslandPage
{
    void Initialize(AppViewModel model);

    /// <summary>Called each time the page becomes the visible tab.</summary>
    void OnShown()
    {
    }
}
