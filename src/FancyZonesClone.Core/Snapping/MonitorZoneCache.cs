using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>
/// Each monitor's description and whether it has zones, keyed by monitor handle, so the low-level keyboard hook can
/// decide whether to take Win+arrow without enumerating display devices, reading the EDID, or building a zone set.
/// Fill it outside the hook with <see cref="Refresh"/>.
/// </summary>
public sealed class MonitorZoneCache
{
    private readonly Dictionary<nint, (MonitorDescriptor Monitor, bool HasZones)> _entries = [];

    /// <summary>Replaces the cache with <paramref name="monitors"/>, asking <paramref name="zones"/> for each one's zones.</summary>
    public void Refresh(IEnumerable<MonitorDescriptor> monitors, IZoneSetSource zones)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(zones);

        _entries.Clear();
        foreach (MonitorDescriptor monitor in monitors)
        {
            _entries[monitor.Handle] = (monitor, !zones.GetZoneSet(monitor).IsEmpty);
        }
    }

    /// <summary>Empties the cache, so every lookup misses until the next <see cref="Refresh"/>.</summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Looks up the monitor with <paramref name="handle"/>. Returns null when it isn't cached, or when its areas no
    /// longer match the cached ones, such as after a resolution change or a moved taskbar: the cache is then stale.
    /// </summary>
    /// <param name="handle">The monitor handle.</param>
    /// <param name="monitorArea">The monitor's current bounds.</param>
    /// <param name="workArea">The monitor's current work area.</param>
    /// <param name="hasZones">Whether the monitor's layout has zones, when it's found.</param>
    public MonitorDescriptor? Find(nint handle, PixelRect monitorArea, PixelRect workArea, out bool hasZones)
    {
        if (_entries.TryGetValue(handle, out (MonitorDescriptor Monitor, bool HasZones) entry) &&
            entry.Monitor.MonitorArea == monitorArea &&
            entry.Monitor.WorkArea == workArea)
        {
            hasZones = entry.HasZones;
            return entry.Monitor;
        }

        hasZones = false;
        return null;
    }
}
