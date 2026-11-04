using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Snapping;

namespace FancyZonesClone.Core.Tests.Snapping;

public class KeyboardSnapperTests
{
    private static readonly MonitorDescriptor _monitor = new(
        7, new MonitorIdentity("AOC3279", "4&1&0&UID3"), new PixelRect(-2560, 0, 0, 1440), new PixelRect(-2560, 0, 0, 1392), 3);

    [Test]
    [Arguments(HorizontalDirection.Left, 2)]
    [Arguments(HorizontalDirection.Right, 0)]
    public async Task NextZoneIndex_UnsnappedWindow_GoesToTheFarZoneForLeftAndFirstForRight(HorizontalDirection direction, int expected)
    {
        // Act
        int? next = KeyboardSnapper.NextZoneIndex([], 3, direction, cycle: false);

        // Assert
        await Assert.That(next).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, HorizontalDirection.Left, 0)]
    [Arguments(1, HorizontalDirection.Right, 2)]
    public async Task NextZoneIndex_MiddleZone_MovesOneZone(int current, HorizontalDirection direction, int expected)
    {
        // Act
        int? next = KeyboardSnapper.NextZoneIndex([current], 3, direction, cycle: false);

        // Assert
        await Assert.That(next).IsEqualTo(expected);
    }

    [Test]
    public async Task NextZoneIndex_LastZoneRightWithCycle_WrapsToFirst()
    {
        // Act
        int? next = KeyboardSnapper.NextZoneIndex([2], 3, HorizontalDirection.Right, cycle: true);

        // Assert
        await Assert.That(next).IsEqualTo(0);
    }

    [Test]
    public async Task NextZoneIndex_FirstZoneLeftWithoutCycle_ReturnsNull()
    {
        // Act
        int? next = KeyboardSnapper.NextZoneIndex([0], 3, HorizontalDirection.Left, cycle: false);

        // Assert
        await Assert.That(next).IsNull();
    }

    [Test]
    public async Task NextZoneIndex_NoZones_ReturnsNull()
    {
        // Act
        int? next = KeyboardSnapper.NextZoneIndex([], 0, HorizontalDirection.Right, cycle: true);

        // Assert
        await Assert.That(next).IsNull();
    }

    [Test]
    public async Task Plan_WindowInFirstOfTwoZones_MovesItToTheSecondZoneOnScreen()
    {
        // Arrange
        var zoneSet = new ZoneSet(ZoneCalculator.Columns(2560, 1392, 2, spacing: 0), sensitivityRadius: 20);
        var assignments = new ZoneAssignments();
        assignments.Assign(42, _monitor.Handle, [0]);

        // Act
        SnapRequest? request = KeyboardSnapper.Plan(42, _monitor, zoneSet, assignments, HorizontalDirection.Right);

        // Assert
        await Assert.That(request!.ZoneIndexes).IsEquivalentTo([1], CollectionOrdering.Matching);
        await Assert.That(request.ScreenRect).IsEqualTo(new PixelRect(-1280, 0, 0, 1392));
    }

    [Test]
    public async Task ZoneAssignments_WindowAssignedOnAnotherMonitor_IsTreatedAsUnsnapped()
    {
        // Arrange
        var assignments = new ZoneAssignments();
        assignments.Assign(42, 1, [2]);

        // Act
        IReadOnlyList<int> zones = assignments.Get(42, 2);

        // Assert
        await Assert.That(zones).IsEmpty();
    }

    [Test]
    public async Task ZoneAssignments_RemoveMonitor_ForgetsOnlyThatMonitorsWindows()
    {
        // Arrange
        var assignments = new ZoneAssignments();
        assignments.Assign(1, 10, [0]);
        assignments.Assign(2, 20, [1]);

        // Act
        assignments.RemoveMonitor(10);

        // Assert
        await Assert.That(assignments.Get(1, 10)).IsEmpty();
        await Assert.That(assignments.Get(2, 20)).IsEquivalentTo([1], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ZoneAssignments_Clear_ForgetsEveryMonitorsWindows()
    {
        // Arrange
        var assignments = new ZoneAssignments();
        assignments.Assign(1, 10, [0]);
        assignments.Assign(2, 20, [1]);

        // Act
        assignments.Clear();

        // Assert
        await Assert.That(assignments.Get(1, 10)).IsEmpty();
        await Assert.That(assignments.Get(2, 20)).IsEmpty();
    }
}
