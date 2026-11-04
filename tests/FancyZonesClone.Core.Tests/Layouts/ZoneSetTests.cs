using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Tests.Layouts;

public class ZoneSetTests
{
    private static readonly ZoneSet _threeColumns = new(ZoneCalculator.Columns(300, 100, 3, spacing: 0), sensitivityRadius: 20);

    // A large zone with a small one inside it, as a canvas layout can draw.
    private static readonly ZoneSet _nested = new(
        [new Zone(0, new PixelRect(0, 0, 400, 400)), new Zone(1, new PixelRect(100, 100, 200, 200))],
        sensitivityRadius: 20);

    [Test]
    public async Task ZonesFromPoint_PointInsideOneZone_ReturnsThatZone()
    {
        // Act
        IReadOnlyList<int> zones = _threeColumns.ZonesFromPoint(new PixelPoint(50, 50), OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(zones).IsEquivalentTo([0], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ZonesFromPoint_PointOnBorderBetweenZones_ReturnsBothNeighbours()
    {
        // Act
        IReadOnlyList<int> zones = _threeColumns.ZonesFromPoint(new PixelPoint(100, 50), OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(zones).IsEquivalentTo([0, 1], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ZonesFromPoint_PointNearButOutsideTheOnlyZone_ReturnsNothing()
    {
        // Arrange
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(100, 100, 200, 200))], sensitivityRadius: 20);

        // Act
        IReadOnlyList<int> zones = zoneSet.ZonesFromPoint(new PixelPoint(90, 150), OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(zones).IsEmpty();
    }

    [Test]
    public async Task ZonesFromPoint_NestedZonesWithSmallest_ReturnsInnerZone()
    {
        // Act
        IReadOnlyList<int> zones = _nested.ZonesFromPoint(new PixelPoint(150, 150), OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(zones).IsEquivalentTo([1], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ZonesFromPoint_NestedZonesWithLargest_ReturnsOuterZone()
    {
        // Act
        IReadOnlyList<int> zones = _nested.ZonesFromPoint(new PixelPoint(150, 150), OverlappingZonesAlgorithm.Largest);

        // Assert
        await Assert.That(zones).IsEquivalentTo([0], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ZonesFromPoint_ClosestCenterNearOuterCentre_ReturnsOuterZoneThatSmallestWouldNot()
    {
        // Arrange
        var zoneSet = new ZoneSet(
            [new Zone(0, new PixelRect(0, 0, 400, 400)), new Zone(1, new PixelRect(0, 0, 200, 200))],
            sensitivityRadius: 20);
        var point = new PixelPoint(190, 190);

        // Act
        IReadOnlyList<int> closest = zoneSet.ZonesFromPoint(point, OverlappingZonesAlgorithm.ClosestCenter);
        IReadOnlyList<int> smallest = zoneSet.ZonesFromPoint(point, OverlappingZonesAlgorithm.Smallest);

        // Assert
        await Assert.That(closest).IsEquivalentTo([0], CollectionOrdering.Matching);
        await Assert.That(smallest).IsEquivalentTo([1], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(100, 0)]
    [Arguments(300, 1)]
    public async Task ZonesFromPoint_PositionalOverIdenticalZones_SplitsOverlapByCursorPosition(int x, int expectedZone)
    {
        // Arrange
        var zoneSet = new ZoneSet(
            [new Zone(0, new PixelRect(0, 0, 400, 400)), new Zone(1, new PixelRect(0, 0, 400, 400))],
            sensitivityRadius: 20);

        // Act
        IReadOnlyList<int> zones = zoneSet.ZonesFromPoint(new PixelPoint(x, 200), OverlappingZonesAlgorithm.Positional);

        // Assert
        await Assert.That(zones).IsEquivalentTo([expectedZone], CollectionOrdering.Matching);
    }

    [Test]
    public async Task CombinedZoneRange_FirstAndThirdOfFourColumns_IncludesTheColumnBetween()
    {
        // Arrange
        var zoneSet = new ZoneSet(ZoneCalculator.Columns(400, 100, 4, spacing: 0), sensitivityRadius: 20);

        // Act
        IReadOnlyList<int> zones = zoneSet.CombinedZoneRange([0], [2]);

        // Assert
        await Assert.That(zones).IsEquivalentTo([0, 1, 2], CollectionOrdering.Matching);
    }

    [Test]
    public async Task CombinedZoneRange_DiagonalCornersOfPriorityGrid_IncludesEveryZoneInTheBoundingBox()
    {
        // Arrange
        var zoneSet = new ZoneSet(ZoneCalculator.PriorityGrid(1000, 500, 4, spacing: 0), sensitivityRadius: 20);

        // Act
        IReadOnlyList<int> zones = zoneSet.CombinedZoneRange([0], [2]);

        // Assert
        await Assert.That(zones).IsEquivalentTo([0, 1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task CombinedRect_TwoAdjacentZones_ReturnsTheirBoundingRectangle()
    {
        // Act
        PixelRect? rect = _threeColumns.CombinedRect([1, 2]);

        // Assert
        await Assert.That(rect).IsEqualTo(new PixelRect(100, 0, 300, 100));
    }

    [Test]
    public async Task CombinedRect_UnknownIndexes_ReturnsNull()
    {
        // Act
        PixelRect? rect = _threeColumns.CombinedRect([7]);

        // Assert
        await Assert.That(rect).IsNull();
    }
}
