using System.Text.Json.Serialization;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Configuration;

/// <summary>
/// Everything FancyZonesClone persists: behaviour, custom layouts, and which layout each monitor uses. The JSON
/// serialiser fills the collections in place, so a key missing from a hand-edited file leaves them empty.
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
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public IList<CustomLayout> CustomLayouts { get; } = [];

    /// <summary>The layout applied to each known monitor.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public IList<MonitorLayoutAssignment> Monitors { get; } = [];

    /// <summary>The layout for a landscape monitor with no assignment of its own.</summary>
    public LayoutSpec DefaultHorizontalLayout { get; set; } = LayoutSpec.CreateDefault();

    /// <summary>The layout for a portrait monitor with no assignment of its own.</summary>
    public LayoutSpec DefaultVerticalLayout { get; set; } = new() { Kind = LayoutType.Rows, ZoneCount = 2 };

    /// <summary>Win+Ctrl+Alt+digit shortcuts that apply a custom layout to the active monitor.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public IList<LayoutHotkey> LayoutHotkeys { get; } = [];
}
