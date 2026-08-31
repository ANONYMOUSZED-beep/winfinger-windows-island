using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinFinger.Services;

public enum PomodoroPhase
{
    Idle,
    Focus,
    Break
}

/// <summary>Pomodoro state machine: focus → break cycles, 1s tick.</summary>
public sealed partial class PomodoroService : ObservableObject
{
    [ObservableProperty] private PomodoroPhase _phase = PomodoroPhase.Idle;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private TimeSpan _remaining;
    [ObservableProperty] private int _focusMinutes = 25;
    [ObservableProperty] private int _breakMinutes = 5;
    [ObservableProperty] private int _completedFocusCount;

    /// <summary>Raised when a phase finishes; argument is the phase that just completed.</summary>
    public event Action<PomodoroPhase>? PhaseCompleted;

    private readonly DispatcherTimer _timer;

    public PomodoroService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        Remaining = TimeSpan.FromMinutes(FocusMinutes);
    }

    public string RemainingText => Remaining.ToString(@"mm\:ss");

    public void StartFocus()
    {
        Phase = PomodoroPhase.Focus;
        Remaining = TimeSpan.FromMinutes(FocusMinutes);
        Resume();
    }

    public void StartBreak()
    {
        Phase = PomodoroPhase.Break;
        Remaining = TimeSpan.FromMinutes(BreakMinutes);
        Resume();
    }

    public void Pause()
    {
        _timer.Stop();
        IsRunning = false;
    }

    public void Resume()
    {
        if (Phase == PomodoroPhase.Idle) return;
        _timer.Start();
        IsRunning = true;
    }

    public void Reset()
    {
        _timer.Stop();
        IsRunning = false;
        Phase = PomodoroPhase.Idle;
        Remaining = TimeSpan.FromMinutes(FocusMinutes);
    }

    partial void OnFocusMinutesChanged(int value)
    {
        if (Phase == PomodoroPhase.Idle)
            Remaining = TimeSpan.FromMinutes(value);
    }

    private void Tick()
    {
        if (Remaining > TimeSpan.FromSeconds(1))
        {
            Remaining -= TimeSpan.FromSeconds(1);
            return;
        }

        Remaining = TimeSpan.Zero;
        _timer.Stop();
        IsRunning = false;
        var finished = Phase;
        if (finished == PomodoroPhase.Focus)
            CompletedFocusCount++;
        PhaseCompleted?.Invoke(finished);

        // auto-advance to the opposite phase
        if (finished == PomodoroPhase.Focus) StartBreak();
        else StartFocus();
    }
}
