namespace FancyZonesClone.Core.Layouts;

/// <summary>The layout applied to a monitor: a template and its parameters, or a reference to a custom layout.</summary>
public sealed class LayoutSpec
{
    /// <summary>The layout family.</summary>
    public LayoutType Kind { get; set; } = LayoutType.PriorityGrid;

    /// <summary>The custom layout to use when <see cref="Kind"/> is <see cref="LayoutType.Custom"/>.</summary>
    public Guid? CustomLayoutId { get; set; }

    /// <summary>Whether to leave <see cref="Spacing"/> pixels between template zones.</summary>
    public bool ShowSpacing { get; set; } = true;

    /// <summary>The gap between template zones, in pixels.</summary>
    public int Spacing { get; set; } = 16;

    /// <summary>How many zones a template generates.</summary>
    public int ZoneCount { get; set; } = 3;

    /// <summary>How far outside a zone, in pixels, the cursor still counts as over it.</summary>
    public int SensitivityRadius { get; set; } = 20;

    /// <summary>FancyZones' out-of-the-box layout: a priority grid of three zones.</summary>
    public static LayoutSpec CreateDefault() => new();

    /// <summary>Returns a layout that applies the given custom layout.</summary>
    public static LayoutSpec ForCustom(Guid customLayoutId) => new() { Kind = LayoutType.Custom, CustomLayoutId = customLayoutId };
}
