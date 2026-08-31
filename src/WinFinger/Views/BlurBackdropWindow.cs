using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WinFinger.Interop;

namespace WinFinger.Views;

/// <summary>
/// Rounded acrylic-blur window slotted directly beneath the island window.
/// Fully click-through and never activated; the island's anti-aliased border
/// covers the region's hard clip edge.
/// </summary>
public sealed class BlurBackdropWindow : Window
{
    private IntPtr _hwnd;
    private readonly IntPtr _islandHwnd;

    public BlurBackdropWindow(IntPtr islandHwnd)
    {
        _islandHwnd = islandHwnd;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Background = System.Windows.Media.Brushes.Transparent;
        Width = 40;
        Height = 20;

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            int style = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE,
                style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT);
            HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
            EnableBlur();
        };
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_NCHITTEST)
        {
            handled = true;
            return new IntPtr(NativeMethods.HTTRANSPARENT);
        }
        return IntPtr.Zero;
    }

    private void EnableBlur()
    {
        // Win11 system acrylic first; legacy blur-behind as fallback for older hosts
        if (!ApplySystemBackdrop())
            ApplyAccent(NativeMethods.ACCENT_ENABLE_BLURBEHIND, 0x00000000);
    }

    private bool ApplySystemBackdrop()
    {
        var margins = new NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        if (NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref margins) != 0) return false;
        int dark = 1;
        NativeMethods.DwmSetWindowAttribute(_hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        int backdrop = NativeMethods.DWMSBT_TRANSIENTWINDOW;
        return NativeMethods.DwmSetWindowAttribute(_hwnd, NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) == 0;
    }

    private bool ApplyAccent(int state, uint gradientAbgr)
    {
        var accent = new NativeMethods.AccentPolicy
        {
            AccentState = state,
            AccentFlags = 2,
            GradientColor = gradientAbgr
        };
        int size = Marshal.SizeOf<NativeMethods.AccentPolicy>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new NativeMethods.WindowCompositionAttribData
            {
                Attribute = NativeMethods.WCA_ACCENT_POLICY,
                Data = ptr,
                SizeOfData = size
            };
            return NativeMethods.SetWindowCompositionAttribute(_hwnd, ref data) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>Device-pixel bounds + rounded clip region, z-slotted just below the island.</summary>
    public void SetGeometry(int x, int y, int width, int height, int cornerRadiusPx)
    {
        if (_hwnd == IntPtr.Zero || width <= 0 || height <= 0) return;
        NativeMethods.SetWindowPos(_hwnd, _islandHwnd, x, y, width, height, NativeMethods.SWP_NOACTIVATE);
        int d = Math.Min(cornerRadiusPx * 2, Math.Min(width, height));
        IntPtr rgn = NativeMethods.CreateRoundRectRgn(0, 0, width + 1, height + 1, d, d);
        NativeMethods.SetWindowRgn(_hwnd, rgn, true); // the system owns rgn from here on
    }

    /// <summary>Move without resizing or rebuilding the region (drag fast path).</summary>
    public void MoveTo(int x, int y)
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.SetWindowPos(_hwnd, _islandHwnd, x, y, 0, 0,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOSIZE);
    }
}
