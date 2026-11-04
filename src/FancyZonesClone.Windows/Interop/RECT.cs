using System.Runtime.InteropServices;
using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly PixelRect ToPixelRect() => new(Left, Top, Right, Bottom);

    public static RECT From(PixelRect rect) => new() { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
}
