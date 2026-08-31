using System.Net.NetworkInformation;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using WinFinger.Interop;

namespace WinFinger.Services;

/// <summary>Samples network throughput and memory load once per second.</summary>
public sealed partial class MetricsService : ObservableObject
{
    [ObservableProperty] private double _downloadBytesPerSecond;
    [ObservableProperty] private double _uploadBytesPerSecond;
    [ObservableProperty] private int _memoryLoadPercent;
    [ObservableProperty] private string _downloadText = "0B";
    [ObservableProperty] private string _uploadText = "0B";
    [ObservableProperty] private string _memoryText = "--%";

    private readonly DispatcherTimer _timer;
    private long _previousReceived;
    private long _previousSent;
    private DateTime _previousSampleAt;
    private bool _hasBaseline;

    public MetricsService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => Sample();
    }

    public void Start()
    {
        Sample(); // establishes baseline; first tick reports 0
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void Sample()
    {
        long received = 0, sent = 0;
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
                var stats = nic.GetIPStatistics();
                received += stats.BytesReceived;
                sent += stats.BytesSent;
            }
        }
        catch
        {
            // Interface enumeration can transiently fail (adapter change); skip this tick.
            return;
        }

        var now = DateTime.UtcNow;
        if (_hasBaseline)
        {
            var elapsed = Math.Max((now - _previousSampleAt).TotalSeconds, 0.2);
            DownloadBytesPerSecond = received >= _previousReceived ? (received - _previousReceived) / elapsed : 0;
            UploadBytesPerSecond = sent >= _previousSent ? (sent - _previousSent) / elapsed : 0;
        }
        _previousReceived = received;
        _previousSent = sent;
        _previousSampleAt = now;
        _hasBaseline = true;

        var status = new NativeMethods.MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };
        if (NativeMethods.GlobalMemoryStatusEx(ref status))
            MemoryLoadPercent = (int)status.dwMemoryLoad;

        DownloadText = FormatRate(DownloadBytesPerSecond);
        UploadText = FormatRate(UploadBytesPerSecond);
        MemoryText = $"{MemoryLoadPercent}%";
    }

    /// <summary>Formats bytes/s compactly: 0 K, 380 K, 2.1 M, 1.2 G.</summary>
    public static string FormatRate(double bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            >= 1 << 30 => $"{bytesPerSecond / (1 << 30):0.#}G",
            >= 1 << 20 => $"{bytesPerSecond / (1 << 20):0.#}M",
            >= 1 << 10 => $"{bytesPerSecond / (1 << 10):0}K",
            _ => $"{bytesPerSecond:0}B"
        };
    }
}
