using System.Runtime.InteropServices;

namespace FancyZonesClone.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAY_DEVICEW
{
    public uint cb;
    public CharBuffer32 DeviceName;
    public CharBuffer128 DeviceString;
    public uint StateFlags;
    public CharBuffer128 DeviceID;
    public CharBuffer128 DeviceKey;
}
