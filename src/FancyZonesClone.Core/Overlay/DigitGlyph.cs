namespace FancyZonesClone.Core.Overlay;

/// <summary>One digit's outline.</summary>
/// <param name="Advance">Distance from this glyph's origin to the next one's, in cap heights.</param>
/// <param name="Left">The leftmost ink, in cap heights from the origin.</param>
/// <param name="Right">The rightmost ink, in cap heights from the origin.</param>
/// <param name="Contours">Closed polygons, each a flat run of x, y pairs.</param>
internal sealed record DigitGlyph(float Advance, float Left, float Right, float[][] Contours);
