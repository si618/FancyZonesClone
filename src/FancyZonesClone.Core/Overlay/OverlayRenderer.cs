using System.Globalization;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Overlay;

/// <summary>
/// Draws zones into a premultiplied BGRA pixel buffer covering the work area. It's managed code, so tests run off
/// Windows and the overlay needs no GDI text, which discards alpha.
/// </summary>
public static class OverlayRenderer
{
    /// <summary>Border thickness, in pixels.</summary>
    public const int BorderThickness = 2;

    // 5x7 bitmap digits, one byte per row, the high five bits used.
    private static readonly byte[][] _digits =
    [
        [0x70, 0x88, 0x98, 0xA8, 0xC8, 0x88, 0x70],
        [0x20, 0x60, 0x20, 0x20, 0x20, 0x20, 0x70],
        [0x70, 0x88, 0x08, 0x10, 0x20, 0x40, 0xF8],
        [0xF8, 0x10, 0x20, 0x10, 0x08, 0x88, 0x70],
        [0x10, 0x30, 0x50, 0x90, 0xF8, 0x10, 0x10],
        [0xF8, 0x80, 0xF0, 0x08, 0x08, 0x88, 0x70],
        [0x30, 0x40, 0x80, 0xF0, 0x88, 0x88, 0x70],
        [0xF8, 0x08, 0x10, 0x20, 0x40, 0x40, 0x40],
        [0x70, 0x88, 0x88, 0x70, 0x88, 0x88, 0x70],
        [0x70, 0x88, 0x88, 0x78, 0x08, 0x10, 0x60],
    ];

    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int GlyphGap = 1;

    /// <summary>
    /// Clears <paramref name="pixels"/> to transparent and draws each zone: a translucent fill, a border, and its
    /// number. Highlighted zones use the highlight colour. Zones are drawn in index order, so later zones cover
    /// earlier ones where they overlap.
    /// </summary>
    public static void Render(Span<uint> pixels, int width, int height, ZoneSet zoneSet, IReadOnlyList<int> highlighted, OverlayStyle style)
    {
        ArgumentNullException.ThrowIfNull(zoneSet);
        ArgumentNullException.ThrowIfNull(highlighted);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixels.Length, width * height, nameof(pixels));

        pixels[..(width * height)].Clear();
        var bounds = new PixelRect(0, 0, width, height);
        uint border = style.Border.ToPremultipliedBgra(byte.MaxValue);
        uint number = style.Number.ToPremultipliedBgra(byte.MaxValue);

        foreach (Zone zone in zoneSet.Zones)
        {
            RgbColor fillColor = highlighted.Contains(zone.Index) ? style.Highlight : style.Zone;
            PixelRect rect = zone.Rect;

            Fill(pixels, width, bounds, rect, fillColor.ToPremultipliedBgra(style.Alpha));
            Fill(pixels, width, bounds, rect with { Bottom = rect.Top + BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Top = rect.Bottom - BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Right = rect.Left + BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Left = rect.Right - BorderThickness }, border);

            if (style.ShowNumbers)
            {
                DrawNumber(pixels, width, bounds, rect, zone.Index + 1, number);
            }
        }
    }

    /// <summary>The pixel scale of the zone number for a zone: about a sixth of its smaller side, at least 2.</summary>
    public static int NumberScale(PixelRect zone) => Math.Max(2, Math.Min(zone.Width, zone.Height) / 6 / GlyphHeight);

    private static void DrawNumber(Span<uint> pixels, int stride, PixelRect bounds, PixelRect zone, int value, uint color)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        int scale = NumberScale(zone);
        int textWidth = ((text.Length * (GlyphWidth + GlyphGap)) - GlyphGap) * scale;
        int originX = zone.Center.X - (textWidth / 2);
        int originY = zone.Center.Y - (GlyphHeight * scale / 2);

        for (int c = 0; c < text.Length; c++)
        {
            byte[] glyph = _digits[text[c] - '0'];
            int glyphX = originX + (c * (GlyphWidth + GlyphGap) * scale);

            for (int row = 0; row < GlyphHeight; row++)
            {
                for (int col = 0; col < GlyphWidth; col++)
                {
                    if ((glyph[row] & (0x80 >> col)) != 0)
                    {
                        var cell = PixelRect.FromSize(glyphX + (col * scale), originY + (row * scale), scale, scale);
                        Fill(pixels, stride, bounds, cell, color);
                    }
                }
            }
        }
    }

    private static void Fill(Span<uint> pixels, int stride, PixelRect bounds, PixelRect rect, uint color)
    {
        PixelRect clipped = rect.Intersect(bounds);
        if (clipped.IsEmpty)
        {
            return;
        }

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            pixels.Slice((y * stride) + clipped.Left, clipped.Width).Fill(color);
        }
    }
}
