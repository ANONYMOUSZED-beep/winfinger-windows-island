namespace WinFinger.Services;

public sealed record IslandNotification(string Icon, string Message);

/// <summary>In-app events surfaced through the island's bulge animation.</summary>
public sealed class NotificationService
{
    public event Action<IslandNotification>? NotificationPosted;

    public void Post(string icon, string message) =>
        NotificationPosted?.Invoke(new IslandNotification(icon, message));
}
