using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Tests.Configuration;

public class LayoutResolverTests
{
    [Test]
    public async Task Resolve_ExactAndDeviceOnlyMatches_PrefersTheExactConnection()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Monitors.Add(Assignment("GSM7863", "4&a&0&UID1", LayoutType.Rows));
        config.Monitors.Add(Assignment("GSM7863", "4&a&0&UID2", LayoutType.Columns));

        // Act
        LayoutSpec layout = LayoutResolver.Resolve(config, Monitor("GSM7863", "4&a&0&UID2", 5120, 2112));

        // Assert
        await Assert.That(layout.Kind).IsEqualTo(LayoutType.Columns);
    }

    [Test]
    public async Task Resolve_MonitorMovedToAnotherPort_FallsBackToTheDeviceMatch()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Monitors.Add(Assignment("AOC3279", "4&a&0&UID9", LayoutType.Focus));

        // Act
        LayoutSpec layout = LayoutResolver.Resolve(config, Monitor("aoc3279", "4&b&0&UID3", 2560, 1392));

        // Assert
        await Assert.That(layout.Kind).IsEqualTo(LayoutType.Focus);
    }

    [Test]
    [Arguments(2560, 1392, LayoutType.PriorityGrid)]
    [Arguments(1440, 2512, LayoutType.Rows)]
    public async Task Resolve_UnknownMonitor_UsesTheDefaultForItsOrientation(int width, int height, LayoutType expected)
    {
        // Arrange
        var config = new FancyZonesCloneConfig();

        // Act
        LayoutSpec layout = LayoutResolver.Resolve(config, Monitor("SAM0ACD", "4&a&0&UID1", width, height));

        // Assert
        await Assert.That(layout.Kind).IsEqualTo(expected);
    }

    [Test]
    public async Task Assign_ExistingExactAssignment_UpdatesItInPlace()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Monitors.Add(Assignment("DEL4233", "4&a&0&UID5", LayoutType.Rows));

        // Act
        LayoutResolver.Assign(config, new MonitorIdentity("DEL4233", "4&a&0&UID5"), new LayoutSpec { Kind = LayoutType.Grid });

        // Assert
        await Assert.That(config.Monitors.Count).IsEqualTo(1);
        await Assert.That(config.Monitors[0].Layout.Kind).IsEqualTo(LayoutType.Grid);
    }

    [Test]
    public async Task Assign_OnlyDeviceMatchExists_AddsAnExactAssignmentAndKeepsTheOther()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Monitors.Add(Assignment("DEL4233", "4&a&0&UID5", LayoutType.Rows));

        // Act
        LayoutResolver.Assign(config, new MonitorIdentity("DEL4233", "4&a&0&UID6"), new LayoutSpec { Kind = LayoutType.Grid });

        // Assert
        await Assert.That(config.Monitors.Count).IsEqualTo(2);
        await Assert.That(config.Monitors[0].Layout.Kind).IsEqualTo(LayoutType.Rows);
    }

    [Test]
    public async Task ZoneSetFor_CustomLayoutAssigned_BuildsItsZones()
    {
        // Arrange
        var custom = new CustomLayout
        {
            Id = Guid.NewGuid(),
            Name = "Thirds",
            Kind = CustomLayoutType.Canvas,
            Canvas = new CanvasLayoutInfo { ReferenceWidth = 300, ReferenceHeight = 100, Zones = [new(0, 0, 100, 100), new(100, 0, 200, 100)] },
        };
        var config = new FancyZonesCloneConfig();
        config.CustomLayouts.Add(custom);
        config.Monitors.Add(new MonitorLayoutAssignment { Monitor = new MonitorIdentity("GSM7863", "x"), Layout = LayoutSpec.ForCustom(custom.Id) });

        // Act
        ZoneSet zoneSet = LayoutResolver.ZoneSetFor(config, Monitor("GSM7863", "x", 600, 200));

        // Assert
        await Assert.That(zoneSet.Zones[1].Rect).IsEqualTo(new PixelRect(200, 0, 600, 200));
    }

    private static MonitorLayoutAssignment Assignment(string device, string instance, LayoutType kind) =>
        new() { Monitor = new MonitorIdentity(device, instance), Layout = new LayoutSpec { Kind = kind } };

    private static MonitorDescriptor Monitor(string device, string instance, int width, int height) =>
        new(1, new MonitorIdentity(device, instance), new PixelRect(0, 0, width, height), new PixelRect(0, 0, width, height), 1);
}
