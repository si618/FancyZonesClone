using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Geometry;

/// <summary>A point in physical pixels.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PixelPoint(int X, int Y)
{
    /// <summary>Returns this point moved by the given offsets.</summary>
    public PixelPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
}
