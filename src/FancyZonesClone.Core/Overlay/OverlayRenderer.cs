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

    // The number badge is a pill this many number heights tall, padded this far either side of the digits.
    private const float BadgeHeight = 1.75f;
    private const float BadgePadding = 0.475f;

    // Opacity of the badge behind a zone number that isn't highlighted, so the highlighted one stands out.
    private const float UnhighlightedBadgeOpacity = 0.42f;

    // A dark outline around the badge, in number heights, so a light badge still has an edge on a light window.
    private const float OutlineWidth = 0.04f;
    private const float OutlineOpacity = 0.35f;
    private const uint OutlineColor = 0xFF000000;

    // Rows sampled per pixel when filling digit outlines. Coverage across each row is exact.
    private const int SubRows = 5;

    /// <summary>
    /// Clears <paramref name="pixels"/> to transparent and draws each zone: a translucent fill, a border, and its
    /// number on a badge. Highlighted zones use the highlight colour, and their badge is opaque. Zones are drawn in
    /// index order, so later zones cover earlier ones where they overlap.
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
        // The badge takes the lighter of the number and zone colours, so it's light on a dark or a light overlay and
        // the highlight-coloured digits read on it.
        uint badge = RgbColor.Lighter(style.Number, style.Zone).ToPremultipliedBgra(byte.MaxValue);
        uint accent = style.Highlight.ToPremultipliedBgra(byte.MaxValue);

        foreach (Zone zone in zoneSet.Zones)
        {
            bool isHighlighted = highlighted.Contains(zone.Index);
            RgbColor fillColor = isHighlighted ? style.Highlight : style.Zone;
            PixelRect rect = zone.Rect;

            Fill(pixels, width, bounds, rect, fillColor.ToPremultipliedBgra(style.Alpha));
            Fill(pixels, width, bounds, rect with { Bottom = rect.Top + BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Top = rect.Bottom - BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Right = rect.Left + BorderThickness }, border);
            Fill(pixels, width, bounds, rect with { Left = rect.Right - BorderThickness }, border);

            if (style.ShowNumbers)
            {
                float badgeOpacity = isHighlighted ? 1 : UnhighlightedBadgeOpacity;
                DrawNumber(pixels, width, bounds, rect, zone.Index + 1, badge, accent, badgeOpacity);
            }
        }
    }

    /// <summary>The height of the zone number for a zone, in pixels: a tenth of its smaller side, at least 12.</summary>
    public static int NumberHeight(PixelRect zone) => Math.Max(MinimumNumberHeight, Math.Min(zone.Width, zone.Height) / 10);

    private static void DrawNumber(Span<uint> pixels, int stride, PixelRect bounds, PixelRect zone, int value, uint badge, uint ink, float badgeOpacity)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        float size = NumberHeight(zone);

        // Centre the ink, not the advances, in the zone.
        (DigitGlyph[] glyphs, float[] origins, float inkLeft, float inkWidth) = LayOut(text);
        float textWidth = inkWidth * size;
        float centerX = zone.Center.X;
        float centerY = zone.Center.Y;
        float originX = centerX - (textWidth / 2) - (inkLeft * size);
        float originY = centerY - (size / 2);

        float radius = BadgeHeight * size / 2;
        float halfBadge = MathF.Max(radius, (textWidth / 2) + (BadgePadding * size));
        float outlineWidth = MathF.Max(1, OutlineWidth * size);
        float outlineRadius = radius + outlineWidth;

        // The pill is a round-capped stroke between its two cap centres. One extra pixel each side holds the
        // anti-aliased edge.
        float capLeft = centerX - halfBadge + radius;
        float capRight = centerX + halfBadge - radius;
        PixelRect box = new PixelRect(
            (int)MathF.Floor(capLeft - outlineRadius) - 1,
            (int)MathF.Floor(centerY - outlineRadius) - 1,
            (int)MathF.Ceiling(capRight + outlineRadius) + 1,
            (int)MathF.Ceiling(centerY + outlineRadius) + 1).Intersect(bounds);
        if (box.IsEmpty)
        {
            return;
        }

        int length = box.Width * box.Height;
        float[] coverage = ArrayPool<float>.Shared.Rent(length);
        try
        {
            Array.Clear(coverage, 0, length);
            // The outline is a band around the badge, so it doesn't darken a translucent badge from behind.
            Stroke(coverage, box, capLeft, centerY, capRight, centerY, outlineRadius, radius - 1);
            Composite(pixels, stride, box, coverage, OutlineColor, OutlineOpacity);

            Array.Clear(coverage, 0, length);
            Stroke(coverage, box, capLeft, centerY, capRight, centerY, radius, float.NegativeInfinity);
            Composite(pixels, stride, box, coverage, badge, badgeOpacity);

            Array.Clear(coverage, 0, length);
            FillGlyphs(coverage, box, glyphs, origins, originX, originY, size);
            Composite(pixels, stride, box, coverage, ink, 1);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(coverage);
        }
    }

    /// <summary>Lays <paramref name="text"/>'s digits out on their advances, in cap heights, and measures their ink.</summary>
    private static (DigitGlyph[] Glyphs, float[] Origins, float InkLeft, float InkWidth) LayOut(string text)
    {
        var glyphs = new DigitGlyph[text.Length];
        float[] origins = new float[text.Length];
        float pen = 0;
        for (int c = 0; c < text.Length; c++)
        {
            glyphs[c] = InterDigits.Glyph(text[c] - '0');
            origins[c] = pen;
            pen += glyphs[c].Advance;
        }

        float inkLeft = glyphs[0].Left;
        return (glyphs, origins, inkLeft, origins[^1] + glyphs[^1].Right - inkLeft);
    }

    /// <summary>
    /// Sets <paramref name="coverage"/> to the nonzero-rule coverage of the glyphs' outlines, placed at
    /// <paramref name="originX"/> plus each origin and scaled by <paramref name="size"/>. Each pixel row is sampled
    /// <see cref="SubRows"/> times, and each sample row's spans cover pixels by their exact overlap.
    /// </summary>
    private static void FillGlyphs(float[] coverage, PixelRect box, DigitGlyph[] glyphs, float[] origins, float originX, float originY, float size)
    {
        int edgeCount = 0;
        foreach (DigitGlyph glyph in glyphs)
        {
            foreach (float[] contour in glyph.Contours)
            {
                edgeCount += contour.Length / 2;
            }
        }

        // Each edge is x0, y0, x1, y1 in pixels; crossings pair an x with a winding direction.
        float[] edges = ArrayPool<float>.Shared.Rent(edgeCount * 4);
        float[] crossX = ArrayPool<float>.Shared.Rent(edgeCount);
        int[] crossDirection = ArrayPool<int>.Shared.Rent(edgeCount);
        try
        {
            Span<float> placed = edges.AsSpan(0, edgeCount * 4);
            PlaceEdges(placed, glyphs, origins, originX, originY, size);

            for (int y = box.Top; y < box.Bottom; y++)
            {
                Span<float> row = coverage.AsSpan((y - box.Top) * box.Width, box.Width);
                for (int s = 0; s < SubRows; s++)
                {
                    FillSampleRow(row, box.Left, y + ((s + 0.5f) / SubRows), placed, crossX, crossDirection);
                }
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(edges);
            ArrayPool<float>.Shared.Return(crossX);
            ArrayPool<int>.Shared.Return(crossDirection);
        }
    }

    /// <summary>Writes each glyph contour's edges into <paramref name="edges"/> as x0, y0, x1, y1 in pixels.</summary>
    private static void PlaceEdges(Span<float> edges, DigitGlyph[] glyphs, float[] origins, float originX, float originY, float size)
    {
        int e = 0;
        for (int g = 0; g < glyphs.Length; g++)
        {
            float glyphX = originX + (origins[g] * size);
            foreach (float[] contour in glyphs[g].Contours)
            {
                int points = contour.Length / 2;
                for (int p = 0; p < points; p++)
                {
                    int next = (p + 1) % points;
                    edges[e++] = glyphX + (contour[2 * p] * size);
                    edges[e++] = originY + (contour[(2 * p) + 1] * size);
                    edges[e++] = glyphX + (contour[2 * next] * size);
                    edges[e++] = originY + (contour[(2 * next) + 1] * size);
                }
            }
        }
    }

    /// <summary>Adds one sample row's share of coverage wherever the winding number at <paramref name="sampleY"/> isn't zero.</summary>
    private static void FillSampleRow(Span<float> row, int left, float sampleY, ReadOnlySpan<float> edges, float[] crossX, int[] crossDirection)
    {
        int crossings = 0;
        for (int i = 0; i < edges.Length; i += 4)
        {
            float y0 = edges[i + 1];
            float y1 = edges[i + 3];
            if ((sampleY >= y0) == (sampleY >= y1))
            {
                continue;
            }

            float x = edges[i] + ((sampleY - y0) * (edges[i + 2] - edges[i]) / (y1 - y0));

            // Insertion sort: a sample row crosses a handful of edges.
            int at = crossings++;
            while (at > 0 && crossX[at - 1] > x)
            {
                crossX[at] = crossX[at - 1];
                crossDirection[at] = crossDirection[at - 1];
                at--;
            }

            crossX[at] = x;
            crossDirection[at] = y1 > y0 ? 1 : -1;
        }

        int winding = 0;
        for (int i = 0; i < crossings - 1; i++)
        {
            winding += crossDirection[i];
            if (winding != 0)
            {
                AddSpan(row, left, crossX[i], crossX[i + 1], 1f / SubRows);
            }
        }
    }

    /// <summary>Adds <paramref name="weight"/> times each pixel's overlap with the span from <paramref name="x0"/> to <paramref name="x1"/>.</summary>
    private static void AddSpan(Span<float> row, int left, float x0, float x1, float weight)
    {
        x0 = Math.Clamp(x0 - left, 0, row.Length);
        x1 = Math.Clamp(x1 - left, 0, row.Length);
        int first = (int)MathF.Floor(x0);
        int last = Math.Min((int)MathF.Ceiling(x1), row.Length);
        for (int x = first; x < last; x++)
        {
            row[x] += (MathF.Min(x + 1, x1) - MathF.Max(x, x0)) * weight;
        }
    }

    /// <summary>
    /// Raises <paramref name="coverage"/> to the coverage of a round-capped stroke from (ax, ay) to (bx, by), sampled
    /// at pixel centres, leaving out points closer than <paramref name="hollow"/> to the centreline. Coverage falls
    /// from 1 to 0 across the pixel at each edge.
    /// </summary>
    private static void Stroke(float[] coverage, PixelRect box, float ax, float ay, float bx, float by, float halfStroke, float hollow)
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
                float distance = MathF.Sqrt((ex * ex) + (ey * ey));
                float alpha = Math.Clamp(MathF.Min(halfStroke + 0.5f - distance, distance - hollow + 0.5f), 0, 1);

                int index = ((y - box.Top) * box.Width) + (x - box.Left);
                coverage[index] = MathF.Max(coverage[index], alpha);
            }
        }
    }

    /// <summary>Blends <paramref name="color"/> into the pixels under <paramref name="box"/> at its coverage times <paramref name="opacity"/>.</summary>
    private static void Composite(Span<uint> pixels, int stride, PixelRect box, float[] coverage, uint color, float opacity)
    {
        for (int y = 0; y < box.Height; y++)
        {
            Span<uint> row = pixels.Slice(((box.Top + y) * stride) + box.Left, box.Width);
            for (int x = 0; x < box.Width; x++)
            {
                float alpha = MathF.Min(coverage[(y * box.Width) + x], 1) * opacity;
                if (alpha > 0)
                {
                    row[x] = Blend(row[x], color, alpha);
                }
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
