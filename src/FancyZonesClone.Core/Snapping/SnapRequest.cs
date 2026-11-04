using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>A window to place in one or more zones.</summary>
/// <param name="Window">The platform's window handle.</param>
/// <param name="Monitor">The monitor the zones are on.</param>
/// <param name="ZoneIndexes">The zones, ordered by index.</param>
/// <param name="ScreenRect">The zones' bounding rectangle, in virtual-screen pixels.</param>
public sealed record SnapRequest(nint Window, MonitorDescriptor Monitor, IReadOnlyList<int> ZoneIndexes, PixelRect ScreenRect);
