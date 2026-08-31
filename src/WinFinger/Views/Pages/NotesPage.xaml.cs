using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WinFinger.Models;
using WinFinger.ViewModels;

namespace WinFinger.Views.Pages;

public partial class NotesPage : UserControl, IIslandPage
{
    private AppViewModel? _model;
    private Note? _current;
    private bool _loadingEditor;
    private readonly DispatcherTimer _saveDebounce;

    public NotesPage()
    {
        InitializeComponent();
        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounce.Tick += (_, _) =>
        {
            _saveDebounce.Stop();
            CommitEditor();
        };
    }

    public void Initialize(AppViewModel model)
    {
        _model = model;
        NoteList.ItemsSource = model.Notes.Notes;

        NewButton.Click += (_, _) => CreateNote();
        NoteList.SelectionChanged += (_, _) => LoadEditor(NoteList.SelectedItem as Note);
        PinButton.Click += (_, _) =>
        {
            if (_current is { } note) model.Notes.TogglePin(note);
        };
        DeleteButton.Click += (_, _) =>
        {
            if (_current is { } note)
            {
                model.Notes.Remove(note);
                LoadEditor(null);
            }
        };

        TitleBox.TextChanged += OnEditorChanged;
        BodyBox.TextChanged += OnEditorChanged;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CreateNote();
                e.Handled = true;
            }
        };
    }

    private void CreateNote()
    {
        if (_model is null) return;
        var note = _model.Notes.Create();
        NoteList.SelectedItem = note;
        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    private void LoadEditor(Note? note)
    {
        // flush pending edits of the previous note before switching
        if (_saveDebounce.IsEnabled)
        {
            _saveDebounce.Stop();
            CommitEditor();
        }

        _current = note;
        _loadingEditor = true;
        try
        {
            if (note is null)
            {
                EditorPane.Visibility = Visibility.Collapsed;
                EmptyHint.Visibility = Visibility.Visible;
                return;
            }
            EditorPane.Visibility = Visibility.Visible;
            EmptyHint.Visibility = Visibility.Collapsed;
            TitleBox.Text = note.Title;
            BodyBox.Text = note.Body;
        }
        finally
        {
            _loadingEditor = false;
        }
    }

    private void OnEditorChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingEditor || _current is null) return;
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void CommitEditor()
    {
        if (_model is null || _current is null) return;
        _model.Notes.Update(_current.Id, TitleBox.Text, BodyBox.Text);
    }
}
