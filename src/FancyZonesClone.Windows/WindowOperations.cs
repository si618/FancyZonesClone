// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/WindowUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using System.Buffers;
using System.Runtime.InteropServices;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Windows;
using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>Reads window facts from Win32 and moves windows into zones.</summary>
public static unsafe class WindowOperations
{
    private const int MaxPath = 260;

    // The longest extended-length path, plus its terminator.
    private const int MaxExtendedPath = 32_768;

    /// <summary>Reads the facts <see cref="WindowRules.Classify"/> needs.</summary>
    public static WindowTraits ReadTraits(nint window)
    {
        uint style = (uint)Native.GetWindowLongPtr(window, Native.GWL_STYLE);
        uint exStyle = (uint)Native.GetWindowLongPtr(window, Native.GWL_EXSTYLE);
        nint owner = Native.GetWindow(window, Native.GW_OWNER);

        return new WindowTraits(
            IsMinimized: Native.IsIconic(window),
            IsVisible: (style & Native.WS_VISIBLE) != 0,
            IsToolWindow: (exStyle & Native.WS_EX_TOOLWINDOW) != 0,
            IsRoot: Native.GetAncestor(window, Native.GA_ROOT) == window,
            IsPopup: (style & Native.WS_POPUP) != 0,
            HasThickFrame: (style & Native.WS_THICKFRAME) != 0,
            HasCaption: (style & Native.WS_CAPTION) == Native.WS_CAPTION,
            HasMinimizeOrMaximizeBox: (style & (Native.WS_MINIMIZEBOX | Native.WS_MAXIMIZEBOX)) != 0,
            HasVisibleOwner: owner != 0 && Native.IsWindowVisible(owner),
            ProcessPath: ProcessPath(window));
    }

    /// <summary>
    /// Returns the window's executable path, or an empty string when it can't be read, such as for an elevated
    /// process when FancyZonesClone isn't elevated.
    /// </summary>
    public static string ProcessPath(nint window)
    {
        Native.GetWindowThreadProcessId(window, out uint processId);
        nint process = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, processId);
        if (process == 0)
        {
            return "";
        }

        try
        {
            // Most paths fit in MAX_PATH. A longer one, up to the 32,767 characters of an extended-length path, gets a
            // pooled buffer, so an excluded app installed deep in a folder tree still matches its exclusion.
            Span<char> buffer = stackalloc char[MaxPath];
            if (TryQueryImageName(process, buffer, out string path, out int error) || error != Native.ERROR_INSUFFICIENT_BUFFER)
            {
                return path;
            }

            char[] rented = ArrayPool<char>.Shared.Rent(MaxExtendedPath);
            try
            {
                _ = TryQueryImageName(process, rented, out path, out _);
                return path;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
        finally
        {
            Native.CloseHandle(process);
        }
    }

    // On failure, the path is empty and the error is the call's last Win32 error. QueryFullProcessImageNameW returns
    // nothing rather than a truncated path when the buffer is too small.
    private static bool TryQueryImageName(nint process, Span<char> buffer, out string path, out int error)
    {
        uint size = (uint)buffer.Length;
        bool succeeded;
        fixed (char* chars = buffer)
        {
            succeeded = Native.QueryFullProcessImageNameW(process, 0, chars, ref size);
        }

        error = succeeded ? 0 : Marshal.GetLastPInvokeError();
        path = succeeded ? buffer[..(int)size].ToString() : "";
        return succeeded;
    }

    /// <summary>
    /// Whether a move/size loop that just started is a resize: the cursor is on a sizing border. Asks the window
    /// with <c>WM_NCHITTEST</c> and a 50 ms timeout, so a hung app can't stall the hook thread.
    /// </summary>
    public static bool IsResizing(nint window, PixelPoint cursor)
    {
        nint lParam = (cursor.Y << 16) | (cursor.X & 0xFFFF);
        return Native.SendMessageTimeoutW(window, Native.WM_NCHITTEST, 0, lParam, Native.SMTO_ABORTIFHUNG, 50, out nint hit) != 0 &&
            hit is >= Native.HTSIZEFIRST and <= Native.HTSIZELAST;
    }

    /// <summary>
    /// Places <paramref name="window"/> so its visible frame fills <paramref name="zone"/>, a virtual-screen rectangle
    /// on <paramref name="monitor"/>. A maximised window is restored first. A minimised one stays minimised but
    /// restores into the zone.
    /// </summary>
    public static bool MoveToZone(nint window, PixelRect zone, MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (!Native.GetWindowRect(window, out RECT windowRect))
        {
            return false;
        }

        PixelRect? frame = Native.DwmGetWindowAttribute(window, Native.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameRect, sizeof(RECT)) == 0
            ? frameRect.ToPixelRect()
            : null;
        bool resizable = ((uint)Native.GetWindowLongPtr(window, Native.GWL_STYLE) & Native.WS_THICKFRAME) != 0;
        PixelRect target = WindowRules.AdjustForFrame(zone, windowRect.ToPixelRect(), frame, resizable);

        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        Native.GetWindowPlacement(window, ref placement);

        if (Native.IsWindowVisible(window))
        {
            if (placement.showCmd is not Native.SW_SHOWMINIMIZED and not Native.SW_MINIMIZE)
            {
                if (placement.showCmd == Native.SW_SHOWMAXIMIZED)
                {
                    placement.flags &= ~Native.WPF_RESTORETOMAXIMIZED;
                }

                placement.showCmd = Native.SW_RESTORE;
            }
        }
        else
        {
            placement.showCmd = Native.SW_HIDE;
        }

        placement.rcNormalPosition = RECT.From(monitor.ToWorkspace(target));
        placement.flags |= Native.WPF_ASYNCWINDOWPLACEMENT;

        // Twice, as FancyZones does: the second call lets Windows rescale a window that crossed to a monitor with a
        // different DPI, which the first call moved but sized at the old scale.
        bool placed = Native.SetWindowPlacement(window, placement);
        placed &= Native.SetWindowPlacement(window, placement);
        return placed;
    }

    /// <summary>
    /// Taps an unassigned key. After the keyboard hook swallows Win+arrow, Windows sees Win pressed and released with
    /// nothing between, and opens the Start menu.
    /// </summary>
    public static void SuppressStartMenu()
    {
        INPUT* inputs = stackalloc INPUT[2];
        inputs[0] = new INPUT { type = Native.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = Native.VK_UNASSIGNED } };
        inputs[1] = new INPUT { type = Native.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = Native.VK_UNASSIGNED, dwFlags = Native.KEYEVENTF_KEYUP } };
        _ = Native.SendInput(2, inputs, sizeof(INPUT));
    }
}
