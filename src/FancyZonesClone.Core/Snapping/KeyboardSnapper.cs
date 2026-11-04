// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/WindowKeyboardSnap.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>Moves a window between zones by index with Win+Left and Win+Right, as FancyZones' default mode does.</summary>
public static class KeyboardSnapper
{
    /// <summary>
    /// Returns the zone a window moves to. An unsnapped window goes to the last zone on Left and the first on Right.
    /// At an edge, the move wraps when <paramref name="cycle"/> is set and otherwise returns null.
    /// </summary>
    public static int? NextZoneIndex(IReadOnlyList<int> currentZones, int zoneCount, HorizontalDirection direction, bool cycle)
    {
        ArgumentNullException.ThrowIfNull(currentZones);

        if (zoneCount <= 0)
        {
            return null;
        }

        int wrapTarget = direction == HorizontalDirection.Left ? zoneCount - 1 : 0;
        if (currentZones.Count == 0)
        {
            return wrapTarget;
        }

        int current = currentZones[0];
        bool atEdge = direction == HorizontalDirection.Left ? current == 0 : current == zoneCount - 1;
        int? wrapped = cycle ? wrapTarget : null;
        return atEdge ? wrapped : direction == HorizontalDirection.Left ? current - 1 : current + 1;
    }

    /// <summary>Builds the snap for a keyboard move of <paramref name="window"/> within <paramref name="monitor"/>.</summary>
    public static SnapRequest? Plan(
        nint window,
        MonitorDescriptor monitor,
        ZoneSet zoneSet,
        ZoneAssignments assignments,
        HorizontalDirection direction)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(zoneSet);
        ArgumentNullException.ThrowIfNull(assignments);

        IReadOnlyList<int> current = assignments.Get(window, monitor.Handle);
        return NextZoneIndex(current, zoneSet.Zones.Count, direction, cycle: true) is { } next && zoneSet.Find(next) is { } zone
            ? new SnapRequest(window, monitor, [next], monitor.ToScreen(zone.Rect))
            : null;
    }
}
