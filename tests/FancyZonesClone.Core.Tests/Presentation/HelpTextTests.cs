using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Core.Tests.Presentation;

public class HelpTextTests
{
    private const string ConfigPath = @"C:\Users\me\AppData\Roaming\FancyZonesClone\config.json";

    [Test]
    public async Task Create_DefaultConfig_DescribesShiftDragAndConfigPath()
    {
        // Act
        string text = Flatten(HelpText.Create(new FancyZonesCloneConfig(), ConfigPath));

        // Assert
        await Assert.That(text).Contains("Hold Shift while you drag a window to show the zones");
        await Assert.That(text).Contains(ConfigPath);
        await Assert.That(text).Contains("turn on overrideSnapHotkeys");
        await Assert.That(text).Contains("Assign digits under layoutHotkeys");
    }

    [Test]
    public async Task Create_ShiftDragOff_SaysShiftHidesZones()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Settings.ShiftDrag = false;

        // Act
        string text = Flatten(HelpText.Create(config, ConfigPath));

        // Assert
        await Assert.That(text).Contains("Zones show whenever you drag a window. Hold Shift to hide them.");
        await Assert.That(text).DoesNotContain("Hold Shift while you drag");
    }

    [Test]
    public async Task Create_OverrideSnapHotkeysOn_DescribesWinArrows()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Settings.OverrideSnapHotkeys = true;

        // Act
        string text = Flatten(HelpText.Create(config, ConfigPath));

        // Assert
        await Assert.That(text).Contains("Win+Left and Win+Right move the active window");
    }

    [Test]
    public async Task Create_LayoutHotkeysAssigned_ListsDigitsInOrder()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.LayoutHotkeys.Add(new LayoutHotkey { Key = 3, LayoutId = Guid.NewGuid() });
        config.LayoutHotkeys.Add(new LayoutHotkey { Key = 1, LayoutId = Guid.NewGuid() });

        // Act
        string text = Flatten(HelpText.Create(config, ConfigPath));

        // Assert
        await Assert.That(text).Contains("Assigned digits: 1, 3.");
    }

    [Test]
    public async Task Create_QuickLayoutSwitchOff_SaysHowToTurnItOn()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.Settings.QuickLayoutSwitch = false;
        config.LayoutHotkeys.Add(new LayoutHotkey { Key = 1, LayoutId = Guid.NewGuid() });

        // Act
        string text = Flatten(HelpText.Create(config, ConfigPath));

        // Assert
        await Assert.That(text).Contains("turn on quickLayoutSwitch");
        await Assert.That(text).DoesNotContain("Assigned digits");
    }

    private static string Flatten(IReadOnlyList<HelpSection> sections) =>
        string.Join('\n', sections.SelectMany(s => s.Items.Prepend(s.Title)));
}
