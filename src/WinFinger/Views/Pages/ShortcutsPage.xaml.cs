using System.ComponentModel;
using System.Windows.Controls;
using WinFinger.Services;
using WinFinger.ViewModels;

namespace WinFinger.Views.Pages;

public partial class ShortcutsPage : UserControl, IIslandPage
{
    private AppViewModel? _model;

    public ShortcutsPage()
    {
        InitializeComponent();
    }

    public void Initialize(AppViewModel model)
    {
        _model = model;
        model.ForegroundApp.PropertyChanged += OnForegroundChanged;
        Refresh();
    }

    public void OnShown() => Refresh();

    private void OnForegroundChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForegroundAppService.ProcessName))
            Refresh();
    }

    private void Refresh()
    {
        if (_model is null) return;
        var set = _model.ShortcutCatalog.SetFor(_model.ForegroundApp.ProcessName);
        AppNameLabel.Text = set.Id == "generic" ? _model.ForegroundApp.DisplayName : set.DisplayName;
        if (set.Id == "generic" && string.IsNullOrEmpty(_model.ForegroundApp.ProcessName))
            AppNameLabel.Text = set.DisplayName;
        GroupList.ItemsSource = set.Groups;
    }
}
