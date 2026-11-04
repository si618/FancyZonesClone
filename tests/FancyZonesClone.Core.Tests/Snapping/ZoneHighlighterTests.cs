using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Snapping;

namespace FancyZonesClone.Core.Tests.Snapping;

public class ZoneHighlighterTests
{
    private static readonly ZoneSet _fourColumns = new(ZoneCalculator.Columns(400, 100, 4, spacing: 0), sensitivityRadius: 20);

    [Test]
    public async Task Update_SelectManyAfterAnchor_HighlightsEverythingBetween()
    {
        // Arrange
        var highlighter = new ZoneHighlighter();
        highlighter.Update(_fourColumns, new PixelPoint(50, 50), selectMany: true, OverlappingZonesAlgorithm.Smallest);

        // Act
        highlighter.Update(_fourColumns, new PixelPoint(350, 50), selectMany: true, OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(highlighter.Zones).IsEquivalentTo([0, 1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Update_SelectManyReleased_DropsTheAnchor()
    {
        // Arrange
        var highlighter = new ZoneHighlighter();
        highlighter.Update(_fourColumns, new PixelPoint(50, 50), selectMany: true, OverlappingZonesAlgorithm.Smallest);

        // Act
        highlighter.Update(_fourColumns, new PixelPoint(350, 50), selectMany: false, OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(highlighter.Zones).IsEquivalentTo([3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Update_SamePointTwice_ReportsChangeOnlyTheFirstTime()
    {
        // Arrange
        var highlighter = new ZoneHighlighter();

        // Act
        bool first = highlighter.Update(_fourColumns, new PixelPoint(150, 50), selectMany: false, OverlappingZonesAlgorithm.Smallest);
        bool second = highlighter.Update(_fourColumns, new PixelPoint(160, 50), selectMany: false, OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }
}
