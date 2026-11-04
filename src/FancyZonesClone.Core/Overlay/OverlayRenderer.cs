using System.Buffers;
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

    /// <summary>The smallest zone number height, in pixels.</summary>
    public const int MinimumNumberHeight = 12;

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

    /// <summary>The height of the zone number for a zone, in pixels: a sixth of its smaller side, at least 12.</summary>
    public static int NumberHeight(PixelRect zone) => Math.Max(MinimumNumberHeight, Math.Min(zone.Width, zone.Height) / 6);

    private static void DrawNumber(Span<uint> pixels, int stride, PixelRect bounds, PixelRect zone, int value, uint color)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        float size = NumberHeight(zone);
        float textWidth = (((text.Length - 1) * StrokeDigits.Advance) + StrokeDigits.Width) * size;
        float originX = zone.Center.X - (textWidth / 2);
        float originY = zone.Center.Y - (size / 2);
        float halfStroke = StrokeDigits.StrokeWidth * size / 2;

        // One extra pixel each side holds the anti-aliased edge.
        PixelRect box = new PixelRect(
            (int)MathF.Floor(originX) - 1,
            (int)MathF.Floor(originY) - 1,
            (int)MathF.Ceiling(originX + textWidth) + 1,
            (int)MathF.Ceiling(originY + size) + 1).Intersect(bounds);
        if (box.IsEmpty)
        {
            return;
        }

        int length = box.Width * box.Height;
        float[] coverage = ArrayPool<float>.Shared.Rent(length);
        try
        {
            Array.Clear(coverage, 0, length);

            for (int c = 0; c < text.Length; c++)
            {
                float glyphX = originX + (c * StrokeDigits.Advance * size);
                foreach (Segment segment in StrokeDigits.Glyph(text[c] - '0'))
                {
                    Stroke(
                        coverage,
                        box,
                        glyphX + (segment.X1 * size),
                        originY + (segment.Y1 * size),
                        glyphX + (segment.X2 * size),
                        originY + (segment.Y2 * size),
                        halfStroke);
                }
            }

            for (int y = 0; y < box.Height; y++)
            {
                Span<uint> row = pixels.Slice(((box.Top + y) * stride) + box.Left, box.Width);
                for (int x = 0; x < box.Width; x++)
                {
                    float alpha = coverage[(y * box.Width) + x];
                    if (alpha > 0)
                    {
                        row[x] = Blend(row[x], color, alpha);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(coverage);
        }
    }

    /// <summary>
    /// Raises <paramref name="coverage"/> to the coverage of a round-capped stroke from (ax, ay) to (bx, by), sampled
    /// at pixel centres. Coverage falls from 1 to 0 across the pixel at the stroke's edge.
    /// </summary>
    private static void Stroke(float[] coverage, PixelRect box, float ax, float ay, float bx, float by, float halfStroke)
    {
        float reach = halfStroke + 1;
        PixelRect area = new PixelRect(
            (int)MathF.Floor(MathF.Min(ax, bx) - reach),
            (int)MathF.Floor(MathF.Min(ay, by) - reach),
            (int)MathF.Ceiling(MathF.Max(ax, bx) + reach),
            (int)MathF.Ceiling(MathF.Max(ay, by) + reach)).Intersect(box);

        float dx = bx - ax;
        float dy = by - ay;
        float lengthSquared = (dx * dx) + (dy * dy);

        for (int y = area.Top; y < area.Bottom; y++)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                float px = x + 0.5f - ax;
                float py = y + 0.5f - ay;
                float t = lengthSquared > 0 ? Math.Clamp(((px * dx) + (py * dy)) / lengthSquared, 0, 1) : 0;
                float ex = px - (t * dx);
                float ey = py - (t * dy);
                float alpha = Math.Clamp(halfStroke + 0.5f - MathF.Sqrt((ex * ex) + (ey * ey)), 0, 1);

                int index = ((y - box.Top) * box.Width) + (x - box.Left);
                coverage[index] = MathF.Max(coverage[index], alpha);
            }
        }
    }

    /// <summary>Composites an opaque premultiplied <paramref name="source"/> over <paramref name="destination"/> at <paramref name="coverage"/>.</summary>
    private static uint Blend(uint destination, uint source, float coverage)
    {
        int alpha = (int)MathF.Round(coverage * byte.MaxValue, MidpointRounding.AwayFromZero);
        if (alpha >= byte.MaxValue)
        {
            return source;
        }

        int inverse = byte.MaxValue - alpha;
        uint result = 0;
        for (int shift = 0; shift < 32; shift += 8)
        {
            int s = (int)((source >> shift) & 0xFF);
            int d = (int)((destination >> shift) & 0xFF);
            result |= (uint)(((s * alpha) + (d * inverse) + 127) / 255) << shift;
        }

        return result;
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
