// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/HighlightedZones.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Snapping;

/// <summary>Tracks the zones highlighted during a drag, including the anchor for a multi-zone span.</summary>
public sealed class ZoneHighlighter
{
    private IReadOnlyList<int> _initial = [];

    /// <summary>The zones currently highlighted, ordered by index.</summary>
    public IReadOnlyList<int> Zones { get; private set; } = [];

    /// <summary>
    /// Recomputes the highlight for <paramref name="point"/>. With <paramref name="selectMany"/>, the first zones
    /// hit become the anchor, and later points extend the selection to everything between the anchor and the point.
    /// </summary>
    /// <returns>Whether the highlighted zones changed.</returns>
    public bool Update(ZoneSet zoneSet, PixelPoint point, bool selectMany, OverlappingZonesAlgorithm algorithm)
    {
        ArgumentNullException.ThrowIfNull(zoneSet);

        IReadOnlyList<int> highlighted = zoneSet.ZonesFromPoint(point, algorithm);
        if (selectMany)
        {
            if (_initial.Count == 0)
            {
                _initial = highlighted;
            }
            else
            {
                highlighted = zoneSet.CombinedZoneRange(_initial, highlighted);
            }
        }
        else
        {
            _initial = [];
        }

        bool changed = !highlighted.SequenceEqual(Zones);
        Zones = highlighted;
        return changed;
    }

    /// <summary>Clears the highlight and the span anchor.</summary>
    public void Reset()
    {
        Zones = [];
        _initial = [];
    }
}
