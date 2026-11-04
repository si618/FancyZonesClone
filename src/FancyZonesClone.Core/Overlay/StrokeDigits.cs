namespace FancyZonesClone.Core.Overlay;

/// <summary>
/// Vector digits for zone numbers, each a set of stroke centrelines in ems: a glyph is <see cref="Width"/> wide and 1
/// tall, with y down. Centrelines sit half a <see cref="StrokeWidth"/> inside the glyph box, so the stroked glyph fills
/// it exactly.
/// </summary>
internal static class StrokeDigits
{
    /// <summary>Glyph width, in ems.</summary>
    public const float Width = 0.56f;

    /// <summary>Distance from one glyph's left edge to the next, in ems.</summary>
    public const float Advance = 0.68f;

    /// <summary>Stroke width, in ems.</summary>
    public const float StrokeWidth = 0.12f;

    // Arcs are split into chords of this many degrees, which keeps the error under half a pixel up to 600px glyphs.
    private const float ArcStepDegrees = 6;

    private static readonly Segment[][] _glyphs =
    [
        Glyph(Arc(0.28f, 0.50f, 0.22f, 0.44f, 0, 360)),
        Glyph(Line(0.30f, 0.06f, 0.30f, 0.94f), Line(0.30f, 0.06f, 0.10f, 0.22f)),
        Glyph(Arc(0.28f, 0.27f, 0.21f, 0.21f, 200, 180), Line(0.477f, 0.342f, 0.06f, 0.94f), Line(0.06f, 0.94f, 0.50f, 0.94f)),
        Glyph(Arc(0.28f, 0.27f, 0.20f, 0.21f, 210, 240), Arc(0.28f, 0.71f, 0.22f, 0.23f, 270, 250)),
        Glyph(Line(0.40f, 0.06f, 0.06f, 0.68f), Line(0.06f, 0.68f, 0.50f, 0.68f), Line(0.40f, 0.06f, 0.40f, 0.94f)),
        Glyph(Line(0.48f, 0.06f, 0.10f, 0.06f), Line(0.10f, 0.06f, 0.107f, 0.496f), Arc(0.27f, 0.68f, 0.23f, 0.26f, 225, 295)),
        Glyph(Arc(0.28f, 0.68f, 0.22f, 0.26f, 0, 360), Arc(0.50f, 0.68f, 0.44f, 0.62f, 270, -90)),
        Glyph(Line(0.06f, 0.06f, 0.50f, 0.06f), Line(0.50f, 0.06f, 0.18f, 0.94f)),
        Glyph(Arc(0.28f, 0.27f, 0.19f, 0.21f, 0, 360), Arc(0.28f, 0.71f, 0.22f, 0.23f, 0, 360)),
        Glyph(Arc(0.28f, 0.32f, 0.22f, 0.26f, 0, 360), Arc(0.06f, 0.32f, 0.44f, 0.62f, 0, 90)),
    ];

    /// <summary>The stroke centrelines of <paramref name="digit"/>, from 0 to 9.</summary>
    public static ReadOnlySpan<Segment> Glyph(int digit) => _glyphs[digit];

    private static Segment[] Glyph(params IEnumerable<Segment>[] strokes) => [.. strokes.SelectMany(stroke => stroke)];

    private static IEnumerable<Segment> Line(float x1, float y1, float x2, float y2)
    {
        yield return new Segment(x1, y1, x2, y2);
    }

    // Angles are in degrees, clockwise from +x because y points down.
    private static IEnumerable<Segment> Arc(float cx, float cy, float rx, float ry, float startDegrees, float sweepDegrees)
    {
        int steps = (int)MathF.Ceiling(MathF.Abs(sweepDegrees) / ArcStepDegrees);
        (float x, float y) = PointOn(startDegrees);

        for (int i = 1; i <= steps; i++)
        {
            (float nextX, float nextY) = PointOn(startDegrees + (sweepDegrees * i / steps));
            yield return new Segment(x, y, nextX, nextY);
            (x, y) = (nextX, nextY);
        }

        (float X, float Y) PointOn(float degrees)
        {
            float radians = degrees * MathF.PI / 180;
            return (cx + (rx * MathF.Cos(radians)), cy + (ry * MathF.Sin(radians)));
        }
    }
}
