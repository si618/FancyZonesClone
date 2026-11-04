// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/MonitorUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>Enumerates the connected monitors and identifies them the way FancyZones does.</summary>
public static unsafe class Monitors
{
    [ThreadStatic]
    private static List<nint>? _enumerated;

    /// <summary>Returns every connected monitor, in the order Windows enumerates them.</summary>
    public static IReadOnlyList<MonitorDescriptor> GetAll()
    {
        _enumerated = [];
        try
        {
            Native.EnumDisplayMonitors(0, 0, &CollectMonitor, 0);
            return [.. _enumerated.Select(Describe).OfType<MonitorDescriptor>()];
        }
        finally
        {
            _enumerated = null;
        }
    }

    /// <summary>Returns the monitor containing <paramref name="point"/>, or the nearest one.</summary>
    public static MonitorDescriptor? FromPoint(PixelPoint point) => Describe(HandleFromPoint(point));

    /// <summary>
    /// Returns the handle of the monitor containing <paramref name="point"/>, or the nearest one. To check whether
    /// the monitor changed, call this: it's cheap, unlike <see cref="Describe"/>, which enumerates display devices.
    /// </summary>
    public static nint HandleFromPoint(PixelPoint point) =>
        Native.MonitorFromPoint(new POINT { X = point.X, Y = point.Y }, Native.MONITOR_DEFAULTTONEAREST);

    /// <summary>Returns the monitor showing most of <paramref name="window"/>.</summary>
    public static MonitorDescriptor? FromWindow(nint window) => Describe(HandleFromWindow(window));

    /// <summary>Returns the handle of the monitor showing most of <paramref name="window"/>. Unlike <see cref="FromWindow"/>, it's cheap.</summary>
    public static nint HandleFromWindow(nint window) => Native.MonitorFromWindow(window, Native.MONITOR_DEFAULTTONEAREST);

    /// <summary>
    /// Reads a monitor's bounds and work area. Unlike <see cref="Describe"/>, it doesn't enumerate display devices or
    /// read the registry, so it's quick enough for a keyboard hook.
    /// </summary>
    public static bool TryGetAreas(nint monitor, out PixelRect monitorArea, out PixelRect workArea)
    {
        var info = new MONITORINFOEXW { cbSize = (uint)sizeof(MONITORINFOEXW) };
        if (monitor == 0 || !Native.GetMonitorInfoW(monitor, ref info))
        {
            monitorArea = default;
            workArea = default;
            return false;
        }

        monitorArea = info.rcMonitor.ToPixelRect();
        workArea = info.rcWork.ToPixelRect();
        return true;
    }

    /// <summary>Describes a monitor handle: identity, bounds, and display number.</summary>
    public static MonitorDescriptor? Describe(nint monitor)
    {
        var info = new MONITORINFOEXW { cbSize = (uint)sizeof(MONITORINFOEXW) };
        if (monitor == 0 || !Native.GetMonitorInfoW(monitor, ref info))
        {
            return null;
        }

        string displayName = CharBuffers.ToTerminatedString(info.szDevice);
        DISPLAY_DEVICEW? device = FindActiveDevice(displayName);

        // With no active display device, fall back to the adapter name, as FancyZones does.
        MonitorIdentity identity = device is { } d
            ? MonitorIdentity.FromDeviceInterfaceName(CharBuffers.ToTerminatedString(d.DeviceID))
            : new MonitorIdentity(displayName, "");
        identity = identity with { SerialNumber = ReadSerialNumber(identity) };
        int number = MonitorIdentity.ParseDisplayNumber(device is { } named ? CharBuffers.ToTerminatedString(named.DeviceName) : displayName);

        return new MonitorDescriptor(monitor, identity, info.rcMonitor.ToPixelRect(), info.rcWork.ToPixelRect(), number);
    }

    private static DISPLAY_DEVICEW? FindActiveDevice(string displayName)
    {
        var device = new DISPLAY_DEVICEW { cb = (uint)sizeof(DISPLAY_DEVICEW) };
        for (uint index = 0; Native.EnumDisplayDevicesW(displayName, index, ref device, Native.EDD_GET_DEVICE_INTERFACE_NAME); index++)
        {
            if ((device.StateFlags & Native.DISPLAY_DEVICE_ACTIVE) != 0 && (device.StateFlags & Native.DISPLAY_DEVICE_MIRRORING_DRIVER) == 0)
            {
                return device;
            }

            device = new DISPLAY_DEVICEW { cb = (uint)sizeof(DISPLAY_DEVICEW) };
        }

        return null;
    }

    // The serial number FancyZones records, from the EDID Windows stores under the monitor's device instance. It's
    // empty when there's no instance to look up or no EDID, and MonitorIdentity.MatchesSerial never matches empty.
    private static string ReadSerialNumber(MonitorIdentity identity)
    {
        if (identity.InstanceId.Length == 0)
        {
            return "";
        }

        string key = $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{identity.DeviceId}\{identity.InstanceId}\Device Parameters";
        uint size = 0;
        if (Native.RegGetValueW(Native.HKEY_LOCAL_MACHINE, key, "EDID", Native.RRF_RT_REG_BINARY, out _, pvData: null, ref size) != Native.ERROR_SUCCESS ||
            size == 0)
        {
            return "";
        }

        byte[] edid = new byte[size];
        fixed (byte* data = edid)
        {
            return Native.RegGetValueW(Native.HKEY_LOCAL_MACHINE, key, "EDID", Native.RRF_RT_REG_BINARY, out _, data, ref size) == Native.ERROR_SUCCESS
                ? Edid.ReadSerialNumber(edid.AsSpan(0, (int)size))
                : "";
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CollectMonitor(nint monitor, nint hdc, RECT* bounds, nint data)
    {
        _enumerated?.Add(monitor);
        return 1;
    }
}
