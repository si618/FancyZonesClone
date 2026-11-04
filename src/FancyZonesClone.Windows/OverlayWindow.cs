using System.Diagnostics;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Overlay;
using FancyZonesClone.Core.Snapping;
using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>
/// The zone overlay: a click-through, never-activated, topmost layered window covering one monitor's work area,
/// drawn from a premultiplied BGRA buffer with <c>UpdateLayeredWindow</c>.
/// </summary>
public sealed unsafe class OverlayWindow(Func<FancyZonesCloneSettings> settings) : IZoneOverlay, IDisposable
{
    private const string ClassName = "FancyZonesClone.Overlay";

    private nint _window;
    private nint _memoryDc;
    private nint _bitmap;
    private nint _previousBitmap;
    private uint* _pixels;
    private int _width;
    private int _height;

    // Whether the current run of failures is already traced, so a drag sampled at 100 Hz traces it once.
    private bool _failureTraced;

    /// <inheritdoc/>
    public void Show(MonitorDescriptor monitor, ZoneSet zoneSet, IReadOnlyList<int> highlighted)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        // An empty work area has nothing to draw, and out of GDI handles or memory there's nothing to draw on: show
        // nothing, and try again on the next call.
        PixelRect area = monitor.WorkArea;
        if (area.Width <= 0 || area.Height <= 0 || !EnsureWindow() || !EnsureSurface(area.Width, area.Height))
        {
            Hide();
            return;
        }

        OverlayRenderer.Render(new Span<uint>(_pixels, _width * _height), _width, _height, zoneSet, highlighted, OverlayStyle.From(settings()));

        nint screenDc = Native.GetDC(0);
        try
        {
            var blend = new BLENDFUNCTION { BlendOp = Native.AC_SRC_OVER, SourceConstantAlpha = 255, AlphaFormat = Native.AC_SRC_ALPHA };
            Native.UpdateLayeredWindow(
                _window, screenDc, new POINT { X = area.Left, Y = area.Top }, new SIZE { Cx = _width, Cy = _height },
                _memoryDc, default, 0, blend, Native.ULW_ALPHA);
        }
        finally
        {
            _ = Native.ReleaseDC(0, screenDc);
        }

        Native.SetWindowPos(_window, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
    }

    /// <inheritdoc/>
    public void Hide()
    {
        if (_window != 0)
        {
            Native.ShowWindow(_window, Native.SW_HIDE);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ReleaseSurface();
        if (_window != 0)
        {
            Native.DestroyWindow(_window);
            _window = 0;
        }
    }

    private bool EnsureWindow()
    {
        if (_window != 0)
        {
            return true;
        }

        nint instance = Native.GetModuleHandleW(lpModuleName: null);
        fixed (char* className = ClassName)
        {
            // The overlay handles no messages, so DefWindowProc is its window procedure.
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = Native.DefWindowProcAddress,
                hInstance = instance,
                lpszClassName = (nint)className,
            };
            Native.RegisterClassExW(windowClass);
        }

        const uint ExStyle = Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST | Native.WS_EX_NOACTIVATE;
        _window = Native.CreateWindowExW(ExStyle, ClassName, "FancyZonesClone overlay", Native.WS_POPUP, 0, 0, 0, 0, 0, 0, instance, 0);
        return _window != 0 || Fail("CreateWindowExW");
    }

    private bool EnsureSurface(int width, int height)
    {
        if (_bitmap != 0 && width == _width && height == _height)
        {
            return true;
        }

        ReleaseSurface();
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = width,
            biHeight = -height, // Negative: top-down rows, matching the renderer's buffer.
            biPlanes = 1,
            biBitCount = 32,
            biCompression = Native.BI_RGB,
        };

        nint screenDc = Native.GetDC(0);
        try
        {
            nint bits = 0;
            nint memoryDc = Native.CreateCompatibleDC(screenDc);
            nint bitmap = memoryDc != 0 ? Native.CreateDIBSection(screenDc, header, Native.DIB_RGB_COLORS, out bits, 0, 0) : 0;
            nint previousBitmap = bitmap != 0 && bits != 0 ? Native.SelectObject(memoryDc, bitmap) : 0;
            if (previousBitmap == 0)
            {
                // Release whatever was created, so a failure doesn't leak the handles it ran short of.
                if (bitmap != 0)
                {
                    Native.DeleteObject(bitmap);
                }

                if (memoryDc != 0)
                {
                    Native.DeleteDC(memoryDc);
                }

                return Fail(memoryDc == 0 ? "CreateCompatibleDC" : bitmap == 0 || bits == 0 ? "CreateDIBSection" : "SelectObject");
            }

            _memoryDc = memoryDc;
            _bitmap = bitmap;
            _previousBitmap = previousBitmap;
            _pixels = (uint*)bits;
            _width = width;
            _height = height;
            _failureTraced = false;
            return true;
        }
        finally
        {
            _ = Native.ReleaseDC(0, screenDc);
        }
    }

    private bool Fail(string call)
    {
        if (!_failureTraced)
        {
            Trace.TraceWarning($"FancyZonesClone: {call} failed, so the zone overlay isn't shown. The process may be out of GDI handles or memory.");
            _failureTraced = true;
        }

        return false;
    }

    private void ReleaseSurface()
    {
        if (_memoryDc != 0)
        {
            Native.SelectObject(_memoryDc, _previousBitmap);
            Native.DeleteDC(_memoryDc);
            _memoryDc = 0;
        }

        if (_bitmap != 0)
        {
            Native.DeleteObject(_bitmap);
            _bitmap = 0;
        }

        _pixels = null;
        _width = 0;
        _height = 0;
    }
}
