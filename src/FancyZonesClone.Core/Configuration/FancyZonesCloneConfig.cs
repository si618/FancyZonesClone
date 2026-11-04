using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Configuration;

/// <summary>
/// Everything FancyZonesClone persists: behaviour, custom layouts, and which layout each monitor uses. The JSON
/// serialiser sets a property only when its key is present, so a key missing from a hand-edited file keeps the
/// default, and <c>RespectNullableAnnotations</c> rejects an explicit null.
/// </summary>
public sealed class FancyZonesCloneConfig
{
    /// <summary>The current file format version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The file format version.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Behaviour and appearance.</summary>
    public FancyZonesCloneSettings Settings { get; set; } = new();

    /// <summary>User-defined layouts.</summary>
    public IList<CustomLayout> CustomLayouts { get; set; } = [];

    /// <summary>The layout applied to each known monitor.</summary>
    public IList<MonitorLayoutAssignment> Monitors { get; set; } = [];

    /// <summary>The layout for a landscape monitor with no assignment of its own.</summary>
    public LayoutSpec DefaultHorizontalLayout { get; set; } = LayoutSpec.CreateDefault();

    /// <summary>The layout for a portrait monitor with no assignment of its own.</summary>
    public LayoutSpec DefaultVerticalLayout { get; set; } = new() { Kind = LayoutType.Rows, ZoneCount = 2 };

    /// <summary>Win+Ctrl+Alt+digit shortcuts that apply a custom layout to the active monitor.</summary>
    public IList<LayoutHotkey> LayoutHotkeys { get; set; } = [];
}
