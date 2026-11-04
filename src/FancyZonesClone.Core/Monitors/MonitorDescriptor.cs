// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/MonitorUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Core.Monitors;

/// <summary>A connected monitor: who it is, and where it is on the virtual screen.</summary>
/// <param name="Handle">The platform's monitor handle, opaque to Core.</param>
/// <param name="Identity">The monitor's identity, used to find its layout.</param>
/// <param name="MonitorArea">The whole monitor, in virtual-screen pixels.</param>
/// <param name="WorkArea">The monitor less the taskbar and app bars, in virtual-screen pixels.</param>
/// <param name="Number">The display number Windows shows in Settings, or 0 when unknown.</param>
public sealed record MonitorDescriptor(nint Handle, MonitorIdentity Identity, PixelRect MonitorArea, PixelRect WorkArea, int Number)
{
    /// <summary>Whether the work area is at least as wide as it is tall.</summary>
    public bool IsHorizontal => WorkArea.Width >= WorkArea.Height;

    /// <summary>Converts a virtual-screen point to work-area coordinates, the space zones live in.</summary>
    public PixelPoint ToWorkArea(PixelPoint screenPoint) => screenPoint.Offset(-WorkArea.Left, -WorkArea.Top);

    /// <summary>Converts a work-area rectangle, such as a zone, to virtual-screen pixels.</summary>
    public PixelRect ToScreen(PixelRect workAreaRect) => workAreaRect.Offset(WorkArea.Left, WorkArea.Top);

    /// <summary>
    /// Converts a virtual-screen rectangle to the workspace coordinates <c>SetWindowPlacement</c> expects, which are
    /// offset by how far this monitor's work area is inset from its edge, such as by a taskbar at the top or left.
    /// </summary>
    public PixelRect ToWorkspace(PixelRect screenRect) =>
        screenRect.Offset(-(WorkArea.Left - MonitorArea.Left), -(WorkArea.Top - MonitorArea.Top));
}
