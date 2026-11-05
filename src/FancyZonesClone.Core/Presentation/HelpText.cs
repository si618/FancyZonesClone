using System.Globalization;
using FancyZonesClone.Core.Configuration;

namespace FancyZonesClone.Core.Presentation;

/// <summary>Builds the help window's text, so it describes the settings in effect rather than the defaults.</summary>
public static class HelpText
{
    /// <summary>The line above the sections.</summary>
    public const string Intro = "FancyZonesClone snaps windows into the zones of each monitor's layout.";

    /// <summary>Describes snapping, hotkeys, the overlay, the tray menu, and the config file.</summary>
    /// <param name="config">The running configuration.</param>
    /// <param name="configPath">Where the config file lives.</param>
    public static IReadOnlyList<HelpSection> Create(FancyZonesCloneConfig config, string configPath)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(configPath);

        FancyZonesCloneSettings settings = config.Settings;
        return
        [
            new("Snapping",
            [
                settings.ShiftDrag
                    ? "Hold Shift while you drag a window to show the zones, then drop it on one."
                    : "Zones show whenever you drag a window. Hold Shift to hide them.",
                "Hold Ctrl as well to span every zone between the first one you hit and the current one.",
                settings.OverrideSnapHotkeys
                    ? "Win+Left and Win+Right move the active window to the previous or next zone."
                    : "To move the active window between zones with Win+Left and Win+Right, turn on overrideSnapHotkeys.",
                LayoutHotkeyItem(config),
                "Windows blocks moving elevated apps' windows, so to snap them, run FancyZonesClone elevated.",
            ]),
            new("Overlay",
            [
                "The overlay draws each zone while you drag and highlights the zones the window will fill.",
                "Set its look with zoneColor, zoneBorderColor, zoneHighlightColor, highlightOpacity, and showZoneNumbers.",
            ]),
            new("Tray menu",
            [
                "Layouts: pick each monitor's layout, with a preview. Clicking the tray icon opens it too.",
                "Pause: stop snapping until you clear it.",
                "Re-import from PowerToys: replace the config with your FancyZones layouts and settings.",
                "Open config file, then Reload config: apply your edits. Snapped windows lose their zones.",
                "About: the version, the licence, and the libraries FancyZonesClone uses.",
            ]),
            new("Config file",
            [
                "It accepts comments and trailing commas. Add custom layouts under customLayouts, or design them in FancyZones and re-import.",
                "To never snap an app, add its executable name to excludedApps.",
            ], configPath),
        ];
    }

    private static string LayoutHotkeyItem(FancyZonesCloneConfig config)
    {
        if (!config.Settings.QuickLayoutSwitch)
        {
            return "To switch layouts with Win+Ctrl+Alt+digit, turn on quickLayoutSwitch.";
        }

        if (config.LayoutHotkeys.Count == 0)
        {
            return "Win+Ctrl+Alt+digit applies a custom layout to the active monitor. Assign digits under layoutHotkeys.";
        }

        string digits = string.Join(", ", config.LayoutHotkeys.Select(h => h.Key).Order().Select(k => k.ToString(CultureInfo.InvariantCulture)));
        return $"Win+Ctrl+Alt+digit applies a custom layout to the active monitor. Assigned digits: {digits}.";
    }
}
