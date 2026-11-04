using System.Runtime.InteropServices;

namespace FancyZonesClone.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}
