using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using WinFinger.Models;
using WinFinger.ViewModels;

namespace WinFinger.Views.Pages;

public partial class ClipboardPage : UserControl, IIslandPage
{
    private AppViewModel? _model;

    public ClipboardPage()
    {
        InitializeComponent();
    }

    public void Initialize(AppViewModel model)
    {
        _model = model;
        EntryList.ItemsSource = model.ClipboardStore.Entries;

        PauseButton.Click += (_, _) =>
        {
            model.ClipboardMonitor.IsPaused = !model.ClipboardMonitor.IsPaused;
            PauseButton.Content = model.ClipboardMonitor.IsPaused ? "Resume" : "Pause";
        };
        ClearButton.Click += (_, _) => model.ClipboardStore.Clear();

        model.ClipboardStore.Entries.CollectionChanged += OnEntriesChanged;
        UpdateEmptyHint();
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyHint();

    private void UpdateEmptyHint() =>
        EmptyHint.Visibility = _model?.ClipboardStore.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void OnCopyEntry(object sender, RoutedEventArgs e)
    {
        if (_model is not null && ((FrameworkElement)sender).Tag is ClipboardEntry entry)
            _model.ClipboardMonitor.CopyToClipboard(entry);
    }

    private void OnDeleteEntry(object sender, RoutedEventArgs e)
    {
        if (_model is not null && ((FrameworkElement)sender).Tag is ClipboardEntry entry)
            _model.ClipboardStore.Remove(entry);
    }
}
