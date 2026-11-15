// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/FancyZonesWindowProcessing.cpp, WindowUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Core.Windows;

/// <summary>FancyZones' rules for which windows to snap, and how to size a window so its visible frame fills a zone.</summary>
public static class WindowRules
{
    /// <summary>Decides whether a window can be snapped, checking in the same order as FancyZones.</summary>
    public static Processability Classify(WindowTraits window, bool allowChildWindows, IEnumerable<string> excludedApps)
    {
        ArgumentNullException.ThrowIfNull(excludedApps);

        // Popups are snapped only when they look like apps, such as Calculator and Telegram, not menus, flyouts, or
        // the tray.
        return window switch
        {
            { IsMinimized: true } => Processability.Minimized,
            { IsVisible: false } => Processability.NotVisible,
            { IsToolWindow: true } => Processability.ToolWindow,
            { IsRoot: false } => Processability.NonRootWindow,
            { IsPopup: true } when !(window.HasThickFrame && (window.HasCaption || window.HasMinimizeOrMaximizeBox)) =>
                Processability.NonProcessablePopupWindow,
            { HasVisibleOwner: true } when !allowChildWindows => Processability.ChildWindow,
            _ when IsExcluded(window.ProcessPath, excludedApps) => Processability.Excluded,
            _ => Processability.Processable,
        };
    }

    /// <summary>
    /// Whether an exclusion matches the executable name in <paramref name="processPath"/>, ignoring case. As in
    /// FancyZones' <c>find_app_name_in_path</c>, the entry's last occurrence must overlap the text after the last
    /// backslash, so <c>code</c> excludes <c>code.exe</c> but not every program under a <c>code</c> folder.
    /// Blank entries never match.
    /// </summary>
    public static bool IsExcluded(string processPath, IEnumerable<string> excludedApps)
    {
        ArgumentNullException.ThrowIfNull(processPath);
        ArgumentNullException.ThrowIfNull(excludedApps);

        int lastSlash = processPath.LastIndexOf('\\', StringComparison.Ordinal);
        return excludedApps
            .Select(e => e.Trim())
            .Where(e => e.Length > 0)
            .Select(e => (Entry: e, Position: processPath.LastIndexOf(e, StringComparison.OrdinalIgnoreCase)))
            .Any(m => m.Position >= 0 && m.Position <= lastSlash + 1 && m.Position + m.Entry.Length > lastSlash);
    }

    /// <summary>
    /// Adjusts <paramref name="zone"/> for the invisible resize borders Windows 10 and 11 draw outside a window's
    /// visible frame, so the visible frame, not the window rectangle, fills the zone. A window that can't be resized
    /// keeps its size and only moves.
    /// </summary>
    /// <param name="zone">The target, in screen pixels.</param>
    /// <param name="windowRect">The window's rectangle from <c>GetWindowRect</c>, including invisible borders.</param>
    /// <param name="frameRect">The visible frame from <c>DWMWA_EXTENDED_FRAME_BOUNDS</c>, when available.</param>
    /// <param name="resizable">Whether the window has <c>WS_SIZEBOX</c>.</param>
    public static PixelRect AdjustForFrame(PixelRect zone, PixelRect windowRect, PixelRect? frameRect, bool resizable)
    {
        PixelRect target = zone;
        if (frameRect is { } frame)
        {
            // The top has no invisible border: the caption starts at the window edge.
            target = new PixelRect(
                zone.Left - (frame.Left - windowRect.Left),
                zone.Top,
                zone.Right - (frame.Right - windowRect.Right),
                zone.Bottom - (frame.Bottom - windowRect.Bottom));
        }

        return resizable
            ? target
            : PixelRect.FromSize(target.Left, target.Top, windowRect.Width, windowRect.Height);
    }
}
