using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Configuration;

/// <summary>Finds and applies the layout for a monitor.</summary>
public static class LayoutResolver
{
    /// <summary>
    /// Returns the layout for <paramref name="monitor"/>. A monitor matches an assignment by device and connection,
    /// then by serial number, then by device alone, so a monitor moved to another port keeps its layout. A monitor
    /// with no match gets the default layout for its orientation.
    /// </summary>
    public static LayoutSpec Resolve(FancyZonesCloneConfig config, MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(monitor);

        return FindAssignment(config, monitor.Identity)?.Layout
            ?? (monitor.IsHorizontal ? config.DefaultHorizontalLayout : config.DefaultVerticalLayout);
    }

    /// <summary>Returns the assignment that best matches <paramref name="identity"/>, if any.</summary>
    public static MonitorLayoutAssignment? FindAssignment(FancyZonesCloneConfig config, MonitorIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(identity);

        return config.Monitors.FirstOrDefault(a => a.Monitor.MatchesDeviceAndInstance(identity))
            ?? config.Monitors.FirstOrDefault(a => a.Monitor.MatchesSerial(identity))
            ?? config.Monitors.FirstOrDefault(a => string.Equals(a.Monitor.DeviceId, identity.DeviceId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Builds the zone set for <paramref name="monitor"/> from its layout.</summary>
    public static ZoneSet ZoneSetFor(FancyZonesCloneConfig config, MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(monitor);

        LayoutSpec layout = Resolve(config, monitor);
        CustomLayout? custom = layout.Kind == LayoutType.Custom ? FindCustomLayout(config, layout.CustomLayoutId) : null;
        return ZoneCalculator.Calculate(layout, custom, monitor.WorkArea.Width, monitor.WorkArea.Height);
    }

    /// <summary>Returns the custom layout with the given ID, if any.</summary>
    public static CustomLayout? FindCustomLayout(FancyZonesCloneConfig config, Guid? id)
    {
        ArgumentNullException.ThrowIfNull(config);

        return id is { } wanted ? config.CustomLayouts.FirstOrDefault(c => c.Id == wanted) : null;
    }

    /// <summary>
    /// Applies <paramref name="layout"/> to the monitor. An exact device-and-connection assignment is updated in
    /// place; otherwise a new, exact one is added, leaving looser matches for other connections untouched.
    /// </summary>
    public static void Assign(FancyZonesCloneConfig config, MonitorIdentity identity, LayoutSpec layout)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(layout);

        MonitorLayoutAssignment? existing = config.Monitors.FirstOrDefault(a => a.Monitor.MatchesDeviceAndInstance(identity));
        if (existing is not null)
        {
            existing.Layout = layout;
            return;
        }

        config.Monitors.Add(new MonitorLayoutAssignment { Monitor = identity, Layout = layout });
    }
}
