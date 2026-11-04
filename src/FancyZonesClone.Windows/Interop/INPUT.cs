using System.Runtime.InteropServices;

namespace FancyZonesClone.Windows.Interop;

// INPUT is a union of MOUSEINPUT, KEYBDINPUT, and HARDWAREINPUT; MOUSEINPUT is the largest member, so pad to it.
[StructLayout(LayoutKind.Explicit)]
internal struct INPUT
{
    [FieldOffset(0)]
    public uint type;

    [FieldOffset(8)]
    public KEYBDINPUT ki;

    [FieldOffset(8)]
    private MouseInputPadding _padding;

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputPadding
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }
}
