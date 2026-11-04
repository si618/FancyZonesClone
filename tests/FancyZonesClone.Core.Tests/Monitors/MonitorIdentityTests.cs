using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Tests.Monitors;

public class MonitorIdentityTests
{
    [Test]
    public async Task FromDeviceInterfaceName_DisplayInterfacePath_SplitsDeviceAndInstance()
    {
        // Act
        var identity = MonitorIdentity.FromDeviceInterfaceName(@"\\?\DISPLAY#GSM1388#4&125707d6&0&UID8388688#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");

        // Assert
        await Assert.That(identity.DeviceId).IsEqualTo("GSM1388");
        await Assert.That(identity.InstanceId).IsEqualTo("4&125707d6&0&UID8388688");
    }

    [Test]
    public async Task FromDeviceInterfaceName_PlainDisplayName_KeepsWholeNameAsDevice()
    {
        // Act
        var identity = MonitorIdentity.FromDeviceInterfaceName(@"\\.\DISPLAY1");

        // Assert
        await Assert.That(identity.DeviceId).IsEqualTo(@"\\.\DISPLAY1");
        await Assert.That(identity.InstanceId).IsEqualTo("");
    }

    [Test]
    public async Task MatchesSerial_BothSerialsEmpty_DoesNotMatch()
    {
        // Arrange
        var first = new MonitorIdentity("DEL4231", "a");
        var second = new MonitorIdentity("DEL4231", "b");

        // Act
        bool matches = first.MatchesSerial(second);

        // Assert
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task MatchesSerial_SameDeviceAndSerialOnDifferentPorts_Matches()
    {
        // Arrange
        var first = new MonitorIdentity("DEL4231", "a", "810KR83");
        var second = new MonitorIdentity("DEL4231", "b", "810kr83");

        // Act
        bool matches = first.MatchesSerial(second);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    [Arguments(@"\\.\DISPLAY2\Monitor0", 2)]
    [Arguments(@"\\.\DISPLAY12", 12)]
    [Arguments("Default_Monitor", 0)]
    public async Task ParseDisplayNumber_DeviceName_ReadsTheAdapterNumber(string deviceName, int expected)
    {
        // Act
        int number = MonitorIdentity.ParseDisplayNumber(deviceName);

        // Assert
        await Assert.That(number).IsEqualTo(expected);
    }

    [Test]
    public async Task ToWorkspace_TaskbarAtTop_ShiftsUpByTheTaskbarHeight()
    {
        // Arrange
        var monitor = new MonitorDescriptor(
            1, new MonitorIdentity("GSM7863", "x"), new PixelRect(0, 0, 5120, 2160), new PixelRect(0, 40, 5120, 2160), 1);

        // Act
        PixelRect workspace = monitor.ToWorkspace(new PixelRect(1485, 40, 3635, 2101));

        // Assert
        await Assert.That(workspace).IsEqualTo(new PixelRect(1485, 0, 3635, 2061));
    }
}
