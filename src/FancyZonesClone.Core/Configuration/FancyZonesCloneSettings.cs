using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Configuration;

/// <summary>Behaviour and appearance settings. Defaults match FancyZones'.</summary>
public sealed class FancyZonesCloneSettings
{
    /// <summary>Show zones only while Shift is held during a drag. When false, Shift hides them instead.</summary>
    public bool ShiftDrag { get; set; } = true;

    /// <summary>Replace Windows' Win+Left and Win+Right snapping with moving between zones.</summary>
    public bool OverrideSnapHotkeys { get; set; }

    /// <summary>Which zone wins when the cursor is over overlapping zones.</summary>
    public OverlappingZonesAlgorithm OverlappingZonesAlgorithm { get; set; } = OverlappingZonesAlgorithm.Smallest;

    /// <summary>Allow Win+Ctrl+Alt+digit to switch the active monitor's layout.</summary>
    public bool QuickLayoutSwitch { get; set; } = true;

    /// <summary>Allow snapping windows that have a visible owner, such as dialogs.</summary>
    public bool AllowChildWindowSnap { get; set; }

    /// <summary>Draw each zone's number in the overlay.</summary>
    public bool ShowZoneNumbers { get; set; } = true;

    /// <summary>Overlay opacity, from 0 to 100.</summary>
    public int HighlightOpacity { get; set; } = 50;

    /// <summary>Fill colour of a zone, as <c>#RRGGBB</c>.</summary>
    public string ZoneColor { get; set; } = "#F5FCFF";

    /// <summary>Border colour of a zone, as <c>#RRGGBB</c>.</summary>
    public string ZoneBorderColor { get; set; } = "#FFFFFF";

    /// <summary>Fill colour of a highlighted zone, as <c>#RRGGBB</c>.</summary>
    public string ZoneHighlightColor { get; set; } = "#0078D7";

    /// <summary>Colour of the zone numbers, as <c>#RRGGBB</c>.</summary>
    public string ZoneNumberColor { get; set; } = "#000000";

    /// <summary>Executable paths, or parts of them, that are never snapped. Matching ignores case.</summary>
    public IList<string> ExcludedApps { get; set; } = [];
}
