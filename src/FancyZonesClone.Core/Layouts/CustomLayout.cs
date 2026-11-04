namespace FancyZonesClone.Core.Layouts;

/// <summary>A user-defined layout.</summary>
public sealed class CustomLayout
{
    /// <summary>The layout's identity. Imported layouts keep their FancyZones UUID.</summary>
    public required Guid Id { get; init; }

    /// <summary>The name shown in the UI.</summary>
    public required string Name { get; set; }

    /// <summary>Whether <see cref="Grid"/> or <see cref="Canvas"/> defines the zones.</summary>
    public required CustomLayoutType Kind { get; init; }

    /// <summary>The grid definition, when <see cref="Kind"/> is <see cref="CustomLayoutType.Grid"/>.</summary>
    public GridLayoutInfo? Grid { get; init; }

    /// <summary>The canvas definition, when <see cref="Kind"/> is <see cref="CustomLayoutType.Canvas"/>.</summary>
    public CanvasLayoutInfo? Canvas { get; init; }

    /// <summary>Whether a grid layout leaves <see cref="Spacing"/> pixels between zones. Canvas layouts ignore it.</summary>
    public bool ShowSpacing { get; init; }

    /// <summary>The gap between grid zones, in pixels.</summary>
    public int Spacing { get; init; }

    /// <summary>How far outside a zone, in pixels, the cursor still counts as over it.</summary>
    public int SensitivityRadius { get; init; } = 20;
}
