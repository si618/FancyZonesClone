using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Core.Tests.Geometry;

public class PixelRectTests
{
    [Test]
    public async Task ToString_ZoneRectangle_ShowsCornersAndSize()
    {
        // Arrange
        var rect = new PixelRect(1485, 40, 3635, 2101);

        // Act
        string text = rect.ToString();

        // Assert
        await Assert.That(text).IsEqualTo("(1485,40)-(3635,2101) 2150×2061");
    }

    [Test]
    public async Task Intersect_DisjointRectangles_IsEmpty()
    {
        // Act
        PixelRect overlap = new PixelRect(0, 0, 10, 10).Intersect(new PixelRect(20, 20, 30, 30));

        // Assert
        await Assert.That(overlap.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Contains_PointOnRightEdge_IsOutsideBecauseEdgesAreExclusive()
    {
        // Act
        bool inside = new PixelRect(0, 0, 10, 10).Contains(new PixelPoint(10, 5));

        // Assert
        await Assert.That(inside).IsFalse();
    }
}
