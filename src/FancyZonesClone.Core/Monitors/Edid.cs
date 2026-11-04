namespace FancyZonesClone.Core.Monitors;

/// <summary>
/// Reads a monitor's EDID, the block it reports over the cable. FancyZones gets the serial number from WMI's
/// <c>WmiMonitorID.SerialNumberID</c>, which Windows reads from the EDID's serial number descriptor. Reading the EDID
/// directly gives the same string without WMI's COM.
/// </summary>
public static class Edid
{
    private const int BaseBlockLength = 128;
    private const int DescriptorLength = 18;
    private const byte SerialNumberTag = 0xFF;

    // The four 18-byte descriptors of the base block.
    private static readonly int[] _descriptorOffsets = [54, 72, 90, 108];

    private static ReadOnlySpan<byte> Header => [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

    /// <summary>
    /// Returns the text of the serial number descriptor, such as <c>CN0F3K7R</c>, or an empty string when the
    /// EDID isn't valid or has no such descriptor. Empty never matches, so a monitor without one falls back to
    /// matching by device.
    /// </summary>
    public static string ReadSerialNumber(ReadOnlySpan<byte> edid)
    {
        if (edid.Length < BaseBlockLength || !edid[..Header.Length].SequenceEqual(Header))
        {
            return "";
        }

        foreach (int offset in _descriptorOffsets)
        {
            // A display descriptor, not a timing, starts with a zero pixel clock and a zero reserved byte.
            ReadOnlySpan<byte> descriptor = edid.Slice(offset, DescriptorLength);
            if (descriptor[0] == 0 && descriptor[1] == 0 && descriptor[2] == 0 && descriptor[3] == SerialNumberTag)
            {
                return DescriptorText(descriptor[5..]);
            }
        }

        return "";
    }

    // Up to 13 ASCII characters, ended by a line feed and padded with spaces.
    private static string DescriptorText(ReadOnlySpan<byte> text)
    {
        int end = text.IndexOf((byte)0x0A);
        ReadOnlySpan<byte> value = end < 0 ? text : text[..end];
        Span<char> chars = stackalloc char[value.Length];
        int length = 0;
        foreach (byte b in value)
        {
            if (b is >= 0x20 and < 0x7F)
            {
                chars[length++] = (char)b;
            }
        }

        return chars[..length].Trim().ToString();
    }
}
