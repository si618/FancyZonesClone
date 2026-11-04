using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>Supplies the zone set for a monitor, from the current configuration.</summary>
public interface IZoneSetSource
{
    /// <summary>Returns the zone set for <paramref name="monitor"/>, which is empty when snapping is off there.</summary>
    public ZoneSet GetZoneSet(MonitorDescriptor monitor);
}
