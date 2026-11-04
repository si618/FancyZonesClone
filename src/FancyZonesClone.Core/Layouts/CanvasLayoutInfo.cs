namespace FancyZonesClone.Core.Layouts;

/// <summary>A canvas layout: free-form zones drawn on a reference work area, scaled to the actual one.</summary>
public sealed class CanvasLayoutInfo
{
    /// <summary>The width of the work area the zones were drawn on.</summary>
    public required int ReferenceWidth { get; init; }

    /// <summary>The height of the work area the zones were drawn on.</summary>
    public required int ReferenceHeight { get; init; }

    /// <summary>The zones, in index order.</summary>
    public required IReadOnlyList<CanvasZone> Zones { get; init; }
}
