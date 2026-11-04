// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/WindowKeyboardSnap.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
namespace FancyZonesClone.Core.Snapping;

/// <summary>Remembers which zones each snapped window occupies, so keyboard moves continue from there.</summary>
public sealed class ZoneAssignments
{
    private readonly Dictionary<nint, (nint Monitor, IReadOnlyList<int> Zones)> _byWindow = [];

    /// <summary>Records that <paramref name="window"/> occupies <paramref name="zones"/> on <paramref name="monitor"/>.</summary>
    public void Assign(nint window, nint monitor, IReadOnlyList<int> zones) => _byWindow[window] = (monitor, zones);

    /// <summary>Returns the zones <paramref name="window"/> occupies on <paramref name="monitor"/>, or none.</summary>
    public IReadOnlyList<int> Get(nint window, nint monitor) =>
        _byWindow.TryGetValue(window, out (nint Monitor, IReadOnlyList<int> Zones) entry) && entry.Monitor == monitor ? entry.Zones : [];

    /// <summary>Forgets <paramref name="window"/>, such as when it closes or is moved by hand.</summary>
    public void Remove(nint window) => _byWindow.Remove(window);

    /// <summary>Forgets every window, such as when the whole configuration is replaced.</summary>
    public void Clear() => _byWindow.Clear();

    /// <summary>Forgets every window on <paramref name="monitor"/>, such as when its layout changes.</summary>
    public void RemoveMonitor(nint monitor)
    {
        foreach (nint window in _byWindow.Where(e => e.Value.Monitor == monitor).Select(e => e.Key).ToList())
        {
            _byWindow.Remove(window);
        }
    }
}
