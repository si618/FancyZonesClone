using System.Runtime.InteropServices;

namespace FancyZonesClone.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct SIZE
{
    public int Cx;
    public int Cy;
}
