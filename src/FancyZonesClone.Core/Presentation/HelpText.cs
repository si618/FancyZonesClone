using System.Globalization;
using System.Text;
using FancyZonesClone.Core.Configuration;

namespace FancyZonesClone.Core.Presentation;

/// <summary>Builds the tray menu's help text, so it describes the settings in effect rather than the defaults.</summary>
public static class HelpText
{
    /// <summary>Describes snapping, hotkeys, the overlay, the tray menu, and the config file.</summary>
    /// <param name="config">The running configuration.</param>
    /// <param name="configPath">Where the config file lives.</param>
    public static string Create(FancyZonesCloneConfig config, string configPath)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(configPath);

        FancyZonesCloneSettings settings = config.Settings;
        var text = new StringBuilder();

        text.Append("FancyZonesClone snaps windows into the zones of each monitor's layout.\n\n");

        text.Append("Snapping\n");
        text.Append(settings.ShiftDrag
            ? "- Hold Shift while you drag a window to show the zones, then drop it on one.\n"
            : "- Zones show whenever you drag a window. Hold Shift to hide them.\n");
        text.Append("- Hold Ctrl as well to span every zone between the first one you hit and the current one.\n");
        text.Append(settings.OverrideSnapHotkeys
            ? "- Win+Left and Win+Right move the active window to the previous or next zone.\n"
            : "- To move the active window between zones with Win+Left and Win+Right, turn on overrideSnapHotkeys.\n");
        text.Append(LayoutHotkeyLine(config));
        text.Append("- Windows blocks moving elevated apps' windows, so to snap them, run FancyZonesClone elevated.\n\n");

        text.Append("Overlay\n");
        text.Append("- The overlay draws each zone while you drag and highlights the zones the window will fill.\n");
        text.Append("- Set its look with zoneColor, zoneBorderColor, zoneHighlightColor, highlightOpacity, and showZoneNumbers.\n\n");

        text.Append("Tray menu\n");
        text.Append("- Layouts: pick each monitor's layout, with a preview. Clicking the tray icon opens it too.\n");
        text.Append("- Pause: stop snapping until you clear it.\n");
        text.Append("- Re-import from PowerToys: replace the config with your FancyZones layouts and settings.\n");
        text.Append("- Open config file, then Reload config: apply your edits. Snapped windows lose their zones.\n\n");

        text.Append("Config file\n");
        text.Append(CultureInfo.InvariantCulture, $"- {configPath}\n");
        text.Append("- It accepts comments and trailing commas. Add custom layouts under customLayouts, or design them in FancyZones and re-import.\n");
        text.Append("- To never snap an app, add its executable name to excludedApps.");

        return text.ToString();
    }

    private static string LayoutHotkeyLine(FancyZonesCloneConfig config)
    {
        if (!config.Settings.QuickLayoutSwitch)
        {
            return "- To switch layouts with Win+Ctrl+Alt+digit, turn on quickLayoutSwitch.\n";
        }

        if (config.LayoutHotkeys.Count == 0)
        {
            return "- Win+Ctrl+Alt+digit applies a custom layout to the active monitor. Assign digits under layoutHotkeys.\n";
        }

        string digits = string.Join(", ", config.LayoutHotkeys.Select(h => h.Key).Order().Select(k => k.ToString(CultureInfo.InvariantCulture)));
        return $"- Win+Ctrl+Alt+digit applies a custom layout to the active monitor. Assigned digits: {digits}.\n";
    }
}
