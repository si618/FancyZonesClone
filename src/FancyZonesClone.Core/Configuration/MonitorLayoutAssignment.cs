using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Configuration;

/// <summary>The layout applied to one monitor.</summary>
public sealed class MonitorLayoutAssignment
{
    /// <summary>The monitor.</summary>
    public required MonitorIdentity Monitor { get; init; }

    /// <summary>Its layout.</summary>
    public required LayoutSpec Layout { get; set; }
}
