using System.Windows.Threading;
using NAudio.Dsp;
using NAudio.Wave;

namespace WinFinger.Services;

/// <summary>WASAPI loopback capture → 8-band spectrum levels for the compact island visualizer.</summary>
public sealed class AudioVisualizerService
{
    public const int BandCount = 8;

    /// <summary>Smoothed band levels 0..1, updated ~30fps on the UI thread.</summary>
    public float[] Levels { get; } = new float[BandCount];

    public event Action? LevelsUpdated;

    private const int FftSize = 2048; // power of two
    private readonly float[] _ring = new float[FftSize];
    private int _ringPos;
    private readonly object _lock = new();

    private WasapiLoopbackCapture? _capture;
    private DispatcherTimer? _timer;

    public bool IsRunning => _capture is not null;

    public void Start()
    {
        if (_capture is not null) return;
        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
        }
        catch
        {
            _capture = null;
            return;
        }

        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => ComputeSpectrum();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        if (_capture is not null)
        {
            try
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.StopRecording();
                _capture.Dispose();
            }
            catch
            {
            }
            _capture = null;
        }
        Array.Clear(Levels);
        LevelsUpdated?.Invoke();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // loopback is IEEE float; average channels into the mono ring buffer
        var format = _capture?.WaveFormat;
        if (format is null) return;
        int channels = format.Channels;
        int frames = e.BytesRecorded / 4 / channels;
        lock (_lock)
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += BitConverter.ToSingle(e.Buffer, (i * channels + c) * 4);
                _ring[_ringPos] = sum / channels;
                _ringPos = (_ringPos + 1) % FftSize;
            }
        }
    }

    private void ComputeSpectrum()
    {
        var complex = new Complex[FftSize];
        lock (_lock)
        {
            for (int i = 0; i < FftSize; i++)
            {
                int idx = (_ringPos + i) % FftSize;
                // Hann window
                float window = 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (FftSize - 1)));
                complex[i].X = _ring[idx] * window;
                complex[i].Y = 0;
            }
        }
        FastFourierTransform.FFT(true, 11, complex); // 2^11 = 2048

        // log-spaced bands over ~40Hz..8kHz (bins 2..380 at 48kHz)
        for (int band = 0; band < BandCount; band++)
        {
            int start = (int)(2 * Math.Pow(190, band / (double)BandCount));
            int end = (int)(2 * Math.Pow(190, (band + 1) / (double)BandCount));
            end = Math.Max(end, start + 1);
            float peak = 0;
            for (int bin = start; bin < end && bin < FftSize / 2; bin++)
            {
                float magnitude = MathF.Sqrt(complex[bin].X * complex[bin].X + complex[bin].Y * complex[bin].Y);
                peak = MathF.Max(peak, magnitude);
            }
            // perceptual scaling
            float level = Math.Clamp(MathF.Sqrt(peak) * 3.2f, 0f, 1f);
            // fast attack, slow decay
            Levels[band] = level > Levels[band]
                ? Levels[band] * 0.3f + level * 0.7f
                : Levels[band] * 0.82f + level * 0.18f;
        }
        LevelsUpdated?.Invoke();
    }
}
