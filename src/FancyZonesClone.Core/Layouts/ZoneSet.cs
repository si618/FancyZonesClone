// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/Layout.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Core.Layouts;

/// <summary>
/// The zones of one layout on one work area, with FancyZones' rules for which zones the cursor selects.
/// Coordinates are relative to the work area's top-left corner.
/// </summary>
public sealed class ZoneSet
{
    // Squared distance under which two zone centres count as the same point for ClosestCenter.
    private const int OverlappingCentersSensitivity = 75;

    private readonly Dictionary<int, Zone> _byIndex;

    /// <summary>Creates a zone set. Zones are ordered by index, as FancyZones' ordered map keeps them.</summary>
    public ZoneSet(IEnumerable<Zone> zones, int sensitivityRadius)
    {
        ArgumentNullException.ThrowIfNull(zones);

        Zones = [.. zones.OrderBy(z => z.Index)];
        _byIndex = Zones.ToDictionary(z => z.Index);
        SensitivityRadius = sensitivityRadius;
    }

    /// <summary>A zone set with no zones, for blank layouts and failures.</summary>
    public static ZoneSet Empty { get; } = new([], 0);

    /// <summary>The zones, ordered by index.</summary>
    public IReadOnlyList<Zone> Zones { get; }

    /// <summary>How far outside a zone, in pixels, the cursor still counts as over it.</summary>
    public int SensitivityRadius { get; }

    /// <summary>Whether the zone set has no zones.</summary>
    public bool IsEmpty => Zones.Count == 0;

    /// <summary>Returns the zone with the given index, if there is one.</summary>
    public Zone? Find(int index) => _byIndex.TryGetValue(index, out Zone zone) ? zone : null;

    /// <summary>
    /// Returns the zones under <paramref name="point"/>. Non-overlapping zones within the sensitivity radius are all
    /// returned, which lets the cursor on a border select both neighbours. When captured zones overlap by more than
    /// the radius, <paramref name="algorithm"/> picks one.
    /// </summary>
    public IReadOnlyList<int> ZonesFromPoint(PixelPoint point, OverlappingZonesAlgorithm algorithm)
    {
        List<Zone> captured = [];
        int strictlyCaptured = 0;
        int radius = SensitivityRadius;

        foreach (Zone zone in Zones)
        {
            PixelRect rect = zone.Rect;
            if (rect.Left - radius <= point.X && point.X <= rect.Right + radius &&
                rect.Top - radius <= point.Y && point.Y <= rect.Bottom + radius)
            {
                captured.Add(zone);
            }

            if (rect.Contains(point))
            {
                strictlyCaptured++;
            }
        }

        // One zone only within the radius isn't selected: the cursor is near it, not on it.
        if (captured.Count == 1 && strictlyCaptured == 0)
        {
            return [];
        }

        if (!AnyOverlap(captured, radius))
        {
            return [.. captured.Select(z => z.Index)];
        }

        Zone chosen = algorithm switch
        {
            OverlappingZonesAlgorithm.Largest => SelectPriority(captured, (a, b) => a.Rect.Area > b.Rect.Area),
            OverlappingZonesAlgorithm.Positional => SelectSubregion(captured, point, radius),
            OverlappingZonesAlgorithm.ClosestCenter => SelectPriority(captured, (a, b) => IsCloserToCenter(a, b, point)),
            OverlappingZonesAlgorithm.Smallest => SelectPriority(captured, (a, b) => a.Rect.Area < b.Rect.Area),
            _ => SelectPriority(captured, (a, b) => a.Rect.Area < b.Rect.Area),
        };

        return [chosen.Index];
    }

    /// <summary>
    /// Returns every zone inside the bounding rectangle of <paramref name="initial"/> and <paramref name="final"/>,
    /// which is how dragging with Ctrl held spans several zones.
    /// </summary>
    public IReadOnlyList<int> CombinedZoneRange(IReadOnlyList<int> initial, IReadOnlyList<int> final)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(final);

        PixelRect? bounds = CombinedRect([.. initial.Union(final)]);
        return bounds is { } box ? [.. Zones.Where(z => box.Contains(z.Rect)).Select(z => z.Index)] : [];
    }

    /// <summary>Returns the bounding rectangle of the given zones, or null when none of them exists.</summary>
    public PixelRect? CombinedRect(IReadOnlyList<int> indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);

        PixelRect? bounds = null;
        foreach (int index in indexes)
        {
            if (_byIndex.TryGetValue(index, out Zone zone))
            {
                bounds = bounds is { } box ? box.Union(zone.Rect) : zone.Rect;
            }
        }

        return bounds;
    }

    private static bool AnyOverlap(List<Zone> zones, int radius)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            for (int j = i + 1; j < zones.Count; j++)
            {
                PixelRect a = zones[i].Rect;
                PixelRect b = zones[j].Rect;
                if (Math.Max(a.Top, b.Top) + radius < Math.Min(a.Bottom, b.Bottom) &&
                    Math.Max(a.Left, b.Left) + radius < Math.Min(a.Right, b.Right))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The first zone that no later zone beats; ties keep the earlier zone.
    private static Zone SelectPriority(List<Zone> zones, Func<Zone, Zone, bool> isBetter)
    {
        Zone chosen = zones[0];
        for (int i = 1; i < zones.Count; i++)
        {
            if (isBetter(zones[i], chosen))
            {
                chosen = zones[i];
            }
        }

        return chosen;
    }

    // Splits the common overlap into equal slices along its longer side, one per zone.
    private static Zone SelectSubregion(List<Zone> zones, PixelPoint point, int radius)
    {
        int half = radius / 2;
        PixelRect overlap = zones[0].Rect.Inflate(half);
        for (int i = 1; i < zones.Count; i++)
        {
            overlap = overlap.Intersect(zones[i].Rect.Inflate(half));
        }

        long width = Math.Max(overlap.Width, 1);
        long height = Math.Max(overlap.Height, 1);
        long slice = height > width
            ? (point.Y - (long)overlap.Top) * zones.Count / height
            : (point.X - (long)overlap.Left) * zones.Count / width;

        return zones[(int)Math.Clamp(slice, 0, zones.Count - 1)];
    }

    private static bool IsCloserToCenter(Zone a, Zone b, PixelPoint point) =>
        SquaredDistance(a.Rect.Center, b.Rect.Center) > OverlappingCentersSensitivity
            ? SquaredDistance(a.Rect.Center, point) < SquaredDistance(b.Rect.Center, point)
            : a.Rect.Area < b.Rect.Area;

    private static long SquaredDistance(PixelPoint a, PixelPoint b)
    {
        long dx = a.X - b.X;
        long dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }
}
