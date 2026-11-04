using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Snapping;

namespace FancyZonesClone.Core.Tests.Snapping;

public class MonitorZoneCacheTests
{
    private static readonly MonitorDescriptor _left = new(
        1, new MonitorIdentity("GSM7863", "4&1&0&UID1"), new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 1);

    private static readonly MonitorDescriptor _right = new(
        2, new MonitorIdentity("DEL4233", "4&1&0&UID2"), new PixelRect(1920, 0, 3840, 1080), new PixelRect(1920, 0, 3840, 1080), 2);

    [Test]
    public async Task Find_CachedMonitorWithSameAreas_ReturnsItAndWhetherItHasZones()
    {
        // Arrange
        var cache = new MonitorZoneCache();
        cache.Refresh([_left, _right], new ZonesOnlyOn(_right.Handle));

        // Act
        MonitorDescriptor? left = cache.Find(_left.Handle, _left.MonitorArea, _left.WorkArea, out bool leftHasZones);
        MonitorDescriptor? right = cache.Find(_right.Handle, _right.MonitorArea, _right.WorkArea, out bool rightHasZones);

        // Assert
        await Assert.That(left).IsEqualTo(_left);
        await Assert.That(leftHasZones).IsFalse();
        await Assert.That(right).IsEqualTo(_right);
        await Assert.That(rightHasZones).IsTrue();
    }

    [Test]
    public async Task Find_WorkAreaChangedSinceRefresh_ReturnsNull()
    {
        // Arrange
        var cache = new MonitorZoneCache();
        cache.Refresh([_left], new ZonesOnlyOn(_left.Handle));

        // Act
        MonitorDescriptor? found = cache.Find(_left.Handle, _left.MonitorArea, new PixelRect(0, 40, 1920, 1080), out bool hasZones);

        // Assert
        await Assert.That(found).IsNull();
        await Assert.That(hasZones).IsFalse();
    }

    [Test]
    public async Task Find_MonitorAreaChangedSinceRefresh_ReturnsNull()
    {
        // Arrange
        var cache = new MonitorZoneCache();
        cache.Refresh([_left], new ZonesOnlyOn(_left.Handle));

        // Act
        MonitorDescriptor? found = cache.Find(_left.Handle, new PixelRect(0, 0, 2560, 1440), _left.WorkArea, out _);

        // Assert
        await Assert.That(found).IsNull();
    }

    [Test]
    public async Task Find_HandleNotCached_ReturnsNull()
    {
        // Arrange
        var cache = new MonitorZoneCache();
        cache.Refresh([_left], new ZonesOnlyOn(_left.Handle));

        // Act
        MonitorDescriptor? found = cache.Find(_right.Handle, _right.MonitorArea, _right.WorkArea, out _);

        // Assert
        await Assert.That(found).IsNull();
    }

    [Test]
    public async Task Refresh_MonitorNoLongerConnected_DropsIt()
    {
        // Arrange
        var cache = new MonitorZoneCache();
        cache.Refresh([_left, _right], new ZonesOnlyOn(_right.Handle));

        // Act
        cache.Refresh([_left], new ZonesOnlyOn(_left.Handle));

        // Assert
        await Assert.That(cache.Find(_right.Handle, _right.MonitorArea, _right.WorkArea, out _)).IsNull();
        await Assert.That(cache.Find(_left.Handle, _left.MonitorArea, _left.WorkArea, out bool hasZones)).IsEqualTo(_left);
        await Assert.That(hasZones).IsTrue();
    }

    private sealed class ZonesOnlyOn(nint handle) : IZoneSetSource
    {
        public ZoneSet GetZoneSet(MonitorDescriptor monitor) =>
            monitor.Handle == handle
                ? ZoneCalculator.Calculate(new LayoutSpec { Kind = LayoutType.Columns, ZoneCount = 2 }, custom: null, monitor.WorkArea.Width, monitor.WorkArea.Height)
                : ZoneSet.Empty;
    }
}
