// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/MonitorUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
namespace FancyZonesClone.Core.Monitors;

/// <summary>
/// Identifies a physical monitor the way FancyZones does: the PnP device ID (such as <c>GSM7863</c>), the instance
/// ID of the connection, and the serial number when known.
/// </summary>
public sealed record MonitorIdentity(string DeviceId, string InstanceId, string SerialNumber = "")
{
    /// <summary>
    /// Parses a device interface name from <c>EnumDisplayDevices</c> with <c>EDD_GET_DEVICE_INTERFACE_NAME</c>,
    /// such as <c>\\?\DISPLAY#GSM1388#4&amp;125707d6&amp;0&amp;UID8388688#{e6f07b5f-...}</c>. A name in another format
    /// becomes the device ID, with no instance ID.
    /// </summary>
    public static MonitorIdentity FromDeviceInterfaceName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        int nameStart = name.IndexOf('#', StringComparison.Ordinal);
        int instanceStart = nameStart < 0 ? -1 : name.IndexOf('#', nameStart + 1, StringComparison.Ordinal);
        int instanceEnd = instanceStart < 0 ? -1 : name.IndexOf('#', instanceStart + 1, StringComparison.Ordinal);

        return instanceEnd < 0
            ? new MonitorIdentity(name, "")
            : new MonitorIdentity(name[(nameStart + 1)..instanceStart], name[(instanceStart + 1)..instanceEnd]);
    }

    /// <summary>
    /// Reads the display number Windows shows in Settings from a device name such as <c>\\.\DISPLAY2\Monitor0</c>
    /// or <c>\\.\DISPLAY2</c>: the digits of the adapter part. Returns 0 when there are none.
    /// </summary>
    public static int ParseDisplayNumber(string deviceName)
    {
        ArgumentNullException.ThrowIfNull(deviceName);

        int monitorPart = deviceName.LastIndexOf(@"\Monitor", StringComparison.OrdinalIgnoreCase);
        ReadOnlySpan<char> adapter = monitorPart < 0 ? deviceName.AsSpan() : deviceName.AsSpan(0, monitorPart);
        int number = 0;
        foreach (char c in adapter)
        {
            if (char.IsAsciiDigit(c))
            {
                number = (number * 10) + (c - '0');
            }
        }

        return number;
    }

    /// <summary>Whether both identities name the same device on the same connection, ignoring case.</summary>
    public bool MatchesDeviceAndInstance(MonitorIdentity other) =>
        other is not null &&
        string.Equals(DeviceId, other.DeviceId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(InstanceId, other.InstanceId, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether both identities carry the same, non-empty serial number and device ID.</summary>
    public bool MatchesSerial(MonitorIdentity other) =>
        other is not null &&
        SerialNumber.Length > 0 &&
        string.Equals(SerialNumber, other.SerialNumber, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(DeviceId, other.DeviceId, StringComparison.OrdinalIgnoreCase);
}
