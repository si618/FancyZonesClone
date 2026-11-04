using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>Shows and hides the zone overlay. The Windows layer draws it; tests record it.</summary>
public interface IZoneOverlay
{
    /// <summary>Shows <paramref name="zoneSet"/> over <paramref name="monitor"/>'s work area, with some zones highlighted.</summary>
    public void Show(MonitorDescriptor monitor, ZoneSet zoneSet, IReadOnlyList<int> highlighted);

    /// <summary>Hides the overlay, if shown.</summary>
    public void Hide();
}
