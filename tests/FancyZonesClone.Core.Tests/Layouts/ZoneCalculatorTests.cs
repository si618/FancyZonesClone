using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Tests.Layouts;

public class ZoneCalculatorTests
{
    [Test]
    public async Task Columns_ThreeZonesWithSpacing_SplitsRemainingWidthEqually()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Columns(1000, 500, 3, spacing: 10);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(10, 10, 330, 490), new PixelRect(340, 10, 660, 490), new PixelRect(670, 10, 990, 490)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task Columns_WidthNotDivisibleByCount_LastZoneEndsExactlyAtWorkAreaEdge()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Columns(100, 50, 3, spacing: 0);

        // Assert
        await Assert.That(zones.Select(z => z.Rect.Right)).IsEquivalentTo([33, 66, 100], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Columns_SpacingWiderThanWorkArea_ReturnsNoZones()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Columns(10, 500, 3, spacing: 16);

        // Assert
        await Assert.That(zones).IsEmpty();
    }

    [Test]
    public async Task Rows_TwoZonesWithSpacing_StacksFullWidthZones()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Rows(1000, 500, 2, spacing: 20);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(20, 20, 980, 240), new PixelRect(20, 260, 980, 480)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task Focus_ThreeZones_CascadesFortyPercentZonesByFiftyPixels()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Focus(1000, 500, 3);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(100, 100, 500, 300), new PixelRect(150, 150, 550, 350), new PixelRect(200, 200, 600, 400)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task PriorityGrid_ThreeZonesNoSpacing_GivesTheMiddleZoneHalfTheWidth()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.PriorityGrid(1000, 500, 3, spacing: 0);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(0, 0, 250, 500), new PixelRect(250, 0, 750, 500), new PixelRect(750, 0, 1000, 500)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task PriorityGrid_ThreeZonesWithSpacing_UsesHalfSpacingBetweenZones()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.PriorityGrid(1000, 500, 3, spacing: 16);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(16, 16, 242, 484), new PixelRect(258, 16, 742, 484), new PixelRect(758, 16, 984, 484)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task PriorityGrid_FourZones_MergesCellsThatShareAnIndex()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.PriorityGrid(1000, 500, 4, spacing: 0);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(0, 0, 250, 500), new PixelRect(250, 0, 750, 500), new PixelRect(750, 0, 1000, 250), new PixelRect(750, 250, 1000, 500)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task PriorityGrid_ElevenZones_FallsBackToEvenGridAsFancyZonesDoes()
    {
        // Arrange
        IReadOnlyList<Zone> evenGrid = ZoneCalculator.EvenGrid(1200, 900, 11, spacing: 0);

        // Act
        IReadOnlyList<Zone> priorityGrid = ZoneCalculator.PriorityGrid(1200, 900, 11, spacing: 0);

        // Assert
        await Assert.That(Rects(priorityGrid)).IsEquivalentTo(Rects(evenGrid), CollectionOrdering.Matching);
    }

    [Test]
    public async Task EvenGrid_FiveZones_LastZoneAbsorbsTheLeftoverCell()
    {
        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.EvenGrid(900, 600, 5, spacing: 0);

        // Assert
        await Assert.That(zones.Count).IsEqualTo(5);
        await Assert.That(zones[4].Rect).IsEqualTo(new PixelRect(299, 300, 900, 600));
    }

    [Test]
    public async Task CalculateGridZones_CustomTwoRowGrid_SplitsByRowPercentages()
    {
        // Arrange
        var grid = new GridLayoutInfo
        {
            Rows = 2,
            Columns = 1,
            RowPercents = [6872, 3128],
            ColumnPercents = [10000],
            CellChildMap = [[0], [1]],
        };

        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.CalculateGridZones(1000, 1000, grid, spacing: 16);

        // Assert
        await Assert.That(Rects(zones)).IsEquivalentTo(
            [new PixelRect(16, 16, 984, 679), new PixelRect(16, 695, 984, 984)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task CalculateGridZones_MapWithWrongRowCount_ReturnsNoZones()
    {
        // Arrange
        var grid = new GridLayoutInfo { Rows = 2, Columns = 1, RowPercents = [5000, 5000], ColumnPercents = [10000], CellChildMap = [[0]] };

        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.CalculateGridZones(1000, 1000, grid, spacing: 0);

        // Assert
        await Assert.That(zones).IsEmpty();
    }

    [Test]
    public async Task Canvas_SameSizeAsReference_KeepsZonesUnchangedHorizontally()
    {
        // Arrange
        var canvas = new CanvasLayoutInfo { ReferenceWidth = 5120, ReferenceHeight = 2160, Zones = [new CanvasZone(0, 0, 1477, 2100)] };

        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Canvas(canvas, 5120, 2112);

        // Assert
        await Assert.That(zones[0].Rect).IsEqualTo(new PixelRect(0, 0, 1477, 2053));
    }

    [Test]
    public async Task Canvas_HalfTheReferenceSize_ScalesAndTruncatesEdges()
    {
        // Arrange
        var canvas = new CanvasLayoutInfo { ReferenceWidth = 5120, ReferenceHeight = 2160, Zones = [new CanvasZone(1485, 0, 2150, 2100)] };

        // Act
        IReadOnlyList<Zone> zones = ZoneCalculator.Canvas(canvas, 2560, 1080);

        // Assert
        await Assert.That(zones[0].Rect).IsEqualTo(new PixelRect(742, 0, 1817, 1050));
    }

    [Test]
    public async Task Calculate_CustomLayoutMissing_ReturnsEmptyZoneSet()
    {
        // Arrange
        var layout = LayoutSpec.ForCustom(Guid.NewGuid());

        // Act
        ZoneSet zoneSet = ZoneCalculator.Calculate(layout, custom: null, 1920, 1080);

        // Assert
        await Assert.That(zoneSet.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Calculate_ShowSpacingOff_IgnoresSpacingValue()
    {
        // Arrange
        var layout = new LayoutSpec { Kind = LayoutType.Columns, ZoneCount = 2, ShowSpacing = false, Spacing = 50 };

        // Act
        ZoneSet zoneSet = ZoneCalculator.Calculate(layout, custom: null, 1000, 500);

        // Assert
        await Assert.That(zoneSet.Zones[0].Rect).IsEqualTo(new PixelRect(0, 0, 500, 500));
    }

    [Test]
    public async Task Calculate_CustomGridLayout_UsesTheCustomLayoutsSensitivityRadius()
    {
        // Arrange
        var custom = new CustomLayout
        {
            Id = Guid.NewGuid(),
            Name = "Halves",
            Kind = CustomLayoutType.Grid,
            Grid = new GridLayoutInfo { Rows = 1, Columns = 2, RowPercents = [10000], ColumnPercents = [5000, 5000], CellChildMap = [[0, 1]] },
            SensitivityRadius = 7,
        };

        // Act
        ZoneSet zoneSet = ZoneCalculator.Calculate(LayoutSpec.ForCustom(custom.Id), custom, 1000, 500);

        // Assert
        await Assert.That(zoneSet.Zones.Count).IsEqualTo(2);
        await Assert.That(zoneSet.SensitivityRadius).IsEqualTo(7);
    }

    [Test]
    public async Task Calculate_ZeroSizedWorkArea_ReturnsEmptyZoneSet()
    {
        // Act
        ZoneSet zoneSet = ZoneCalculator.Calculate(LayoutSpec.CreateDefault(), custom: null, 0, 1080);

        // Assert
        await Assert.That(zoneSet.IsEmpty).IsTrue();
    }

    private static PixelRect[] Rects(IEnumerable<Zone> zones) => [.. zones.Select(z => z.Rect)];
}
