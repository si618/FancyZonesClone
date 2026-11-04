namespace FancyZonesClone.Core.Layouts;

/// <summary>The layout families FancyZones offers. Every kind but <see cref="Custom"/> is generated from a zone count.</summary>
public enum LayoutType
{
    /// <summary>No zones: snapping is off on the monitor.</summary>
    Blank,

    /// <summary>Overlapping, cascaded zones.</summary>
    Focus,

    /// <summary>Equal-width columns.</summary>
    Columns,

    /// <summary>Equal-height rows.</summary>
    Rows,

    /// <summary>An even grid.</summary>
    Grid,

    /// <summary>A grid that gives the middle zone priority. It's FancyZones' default layout.</summary>
    PriorityGrid,

    /// <summary>A user-defined grid or canvas layout, referenced by <see cref="LayoutSpec.CustomLayoutId"/>.</summary>
    Custom,
}
