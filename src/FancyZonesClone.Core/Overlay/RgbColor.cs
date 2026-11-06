using System.Globalization;
using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Overlay;

/// <summary>An opaque RGB colour.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    /// <summary>Parses <c>#RRGGBB</c> or <c>RRGGBB</c>, returning <paramref name="fallback"/> for anything else.</summary>
    public static RgbColor Parse(string? text, RgbColor fallback)
    {
        ReadOnlySpan<char> hex = (text ?? "").AsSpan().Trim().TrimStart('#');
        return hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value)
            ? new RgbColor((byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : fallback;
    }

    /// <summary>
    /// Returns whichever of <paramref name="first"/> and <paramref name="second"/> is lighter, by Rec. 709 luma, and
    /// <paramref name="first"/> on a tie.
    /// </summary>
    public static RgbColor Lighter(RgbColor first, RgbColor second) => second.Luma > first.Luma ? second : first;

    /// <summary>Packs the colour as a premultiplied BGRA pixel, the format <c>UpdateLayeredWindow</c> takes.</summary>
    public uint ToPremultipliedBgra(byte alpha) =>
        ((uint)alpha << 24) | ((uint)Premultiply(R, alpha) << 16) | ((uint)Premultiply(G, alpha) << 8) | Premultiply(B, alpha);

    private float Luma => (0.2126f * R) + (0.7152f * G) + (0.0722f * B);

    private static byte Premultiply(byte channel, byte alpha) => (byte)(((channel * alpha) + 127) / 255);
}
