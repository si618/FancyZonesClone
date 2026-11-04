// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/WindowUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Windows;
using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>Reads window facts from Win32 and moves windows into zones.</summary>
public static unsafe class WindowOperations
{
    /// <summary>Reads the facts <see cref="WindowRules.Classify"/> needs.</summary>
    public static WindowTraits ReadTraits(nint window) => ReadTraits(window, ProcessPath(window));

    /// <summary>
    /// Reads the facts <see cref="WindowRules.Classify"/> needs, with a process path read earlier. Every call it makes
    /// reads window state without opening the process or sending a message, so it's quick enough for a keyboard hook.
    /// </summary>
    public static WindowTraits ReadTraits(nint window, string processPath)
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
            ProcessPath: processPath);
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
            var buffer = default(CharBuffer260);
            uint size = 260;
            return Native.QueryFullProcessImageNameW(process, 0, ref buffer, ref size)
                ? ((ReadOnlySpan<char>)buffer)[..(int)size].ToString()
                : "";
        }
        finally
        {
            Native.CloseHandle(process);
        }
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
