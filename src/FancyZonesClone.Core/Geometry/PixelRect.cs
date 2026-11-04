using System.Globalization;
using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Geometry;

/// <summary>
/// A rectangle in physical pixels, with exclusive right and bottom edges, the same convention as Win32 <c>RECT</c>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    /// <summary>The distance from <see cref="Left"/> to <see cref="Right"/>.</summary>
    public int Width => Right - Left;

    /// <summary>The distance from <see cref="Top"/> to <see cref="Bottom"/>.</summary>
    public int Height => Bottom - Top;

    /// <summary>The area, treating a negative width or height as zero.</summary>
    public long Area => (long)Math.Max(Width, 0) * Math.Max(Height, 0);

    /// <summary>The centre, rounded towards zero, as FancyZones computes it.</summary>
    public PixelPoint Center => new((Left + Right) / 2, (Top + Bottom) / 2);

    /// <summary>Whether the width or height is zero or negative.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>Creates a rectangle from an origin and a size.</summary>
    public static PixelRect FromSize(int left, int top, int width, int height) => new(left, top, left + width, top + height);

    /// <summary>Whether <paramref name="point"/> lies inside, with the right and bottom edges excluded.</summary>
    public bool Contains(PixelPoint point) => Left <= point.X && point.X < Right && Top <= point.Y && point.Y < Bottom;

    /// <summary>Whether <paramref name="other"/> lies entirely inside this rectangle.</summary>
    public bool Contains(PixelRect other) =>
        Left <= other.Left && other.Right <= Right && Top <= other.Top && other.Bottom <= Bottom;

    /// <summary>Returns the smallest rectangle that contains both rectangles.</summary>
    public PixelRect Union(PixelRect other) =>
        new(Math.Min(Left, other.Left), Math.Min(Top, other.Top), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));

    /// <summary>Returns the overlap of both rectangles, which is empty when they don't overlap.</summary>
    public PixelRect Intersect(PixelRect other) =>
        new(Math.Max(Left, other.Left), Math.Max(Top, other.Top), Math.Min(Right, other.Right), Math.Min(Bottom, other.Bottom));

    /// <summary>Returns this rectangle moved by the given offsets.</summary>
    public PixelRect Offset(int dx, int dy) => new(Left + dx, Top + dy, Right + dx, Bottom + dy);

    /// <summary>Formats as <c>(left,top)-(right,bottom) width×height</c>, for logs and diagnostics.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({Left},{Top})-({Right},{Bottom}) {Width}×{Height}");

    /// <summary>Returns this rectangle grown by <paramref name="amount"/> on every side.</summary>
    public PixelRect Inflate(int amount) => new(Left - amount, Top - amount, Right + amount, Bottom + amount);
}
