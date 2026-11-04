using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Windows;

namespace FancyZonesClone.Core.Tests.Windows;

public class WindowRulesTests
{
    private static readonly WindowTraits _appWindow = new(
        IsMinimized: false,
        IsVisible: true,
        IsToolWindow: false,
        IsRoot: true,
        IsPopup: false,
        HasThickFrame: true,
        HasCaption: true,
        HasMinimizeOrMaximizeBox: true,
        HasVisibleOwner: false,
        ProcessPath: @"C:\Program Files\Mozilla Firefox\firefox.exe");

    [Test]
    public async Task Classify_OrdinaryAppWindow_IsProcessable()
    {
        // Act
        Processability result = WindowRules.Classify(_appWindow, allowChildWindows: false, []);

        // Assert
        await Assert.That(result).IsEqualTo(Processability.Processable);
    }

    [Test]
    public async Task Classify_MinimisedToolWindow_ReportsMinimisedFirst()
    {
        // Arrange
        WindowTraits window = _appWindow with { IsMinimized = true, IsToolWindow = true };

        // Act
        Processability result = WindowRules.Classify(window, allowChildWindows: false, []);

        // Assert
        await Assert.That(result).IsEqualTo(Processability.Minimized);
    }

    [Test]
    public async Task Classify_PopupWithoutResizableFrame_IsNotProcessable()
    {
        // Arrange
        WindowTraits menu = _appWindow with { IsPopup = true, HasThickFrame = false };

        // Act
        Processability result = WindowRules.Classify(menu, allowChildWindows: false, []);

        // Assert
        await Assert.That(result).IsEqualTo(Processability.NonProcessablePopupWindow);
    }

    [Test]
    public async Task Classify_PopupWithFrameAndCaption_IsProcessableLikeCalculator()
    {
        // Arrange
        WindowTraits calculator = _appWindow with { IsPopup = true };

        // Act
        Processability result = WindowRules.Classify(calculator, allowChildWindows: false, []);

        // Assert
        await Assert.That(result).IsEqualTo(Processability.Processable);
    }

    [Test]
    [Arguments(false, Processability.ChildWindow)]
    [Arguments(true, Processability.Processable)]
    public async Task Classify_OwnedDialog_DependsOnChildWindowSetting(bool allowChildWindows, Processability expected)
    {
        // Arrange
        WindowTraits dialog = _appWindow with { HasVisibleOwner = true };

        // Act
        Processability result = WindowRules.Classify(dialog, allowChildWindows, []);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Classify_ProcessInExclusionList_IsExcludedIgnoringCase()
    {
        // Act
        Processability result = WindowRules.Classify(_appWindow, allowChildWindows: false, ["FIREFOX.EXE"]);

        // Assert
        await Assert.That(result).IsEqualTo(Processability.Excluded);
    }

    [Test]
    public async Task IsExcluded_BlankEntry_MatchesNothing()
    {
        // Act
        bool excluded = WindowRules.IsExcluded(@"C:\Windows\notepad.exe", ["  "]);

        // Assert
        await Assert.That(excluded).IsFalse();
    }

    [Test]
    [Arguments(@"C:\Program Files\Microsoft VS Code\Code.exe", "code", true)]
    [Arguments(@"C:\Program Files\Microsoft VS Code\Code.exe", "code.exe", true)]
    [Arguments(@"C:\Program Files\Microsoft VS Code\Code.exe", "Microsoft VS Code\\Code.exe", true)]
    [Arguments(@"C:\Users\me\code\tool.exe", "code", false)]
    [Arguments(@"C:\Program Files\Microsoft Office\winword.exe", "Microsoft", false)]
    [Arguments(@"C:\Windows\notepad.exe", "pad", false)]
    public async Task IsExcluded_EntryAgainstPath_MatchesFromTheStartOfTheExecutableName(string path, string entry, bool expected)
    {
        // Act
        bool excluded = WindowRules.IsExcluded(path, [entry]);

        // Assert
        await Assert.That(excluded).IsEqualTo(expected);
    }

    [Test]
    public async Task IsExcluded_EntryOccursInFolderAndFileName_MatchesOnTheLastOccurrence()
    {
        // Arrange: FancyZones tests the entry's last occurrence, which here is the file name.
        const string Path = @"C:\tools\git\git.exe";

        // Act
        bool excluded = WindowRules.IsExcluded(Path, ["git"]);

        // Assert
        await Assert.That(excluded).IsTrue();
    }

    [Test]
    public async Task AdjustForFrame_WindowWithInvisibleBorders_ExpandsSoVisibleFrameFillsZone()
    {
        // Arrange
        var windowRect = new PixelRect(93, 100, 1007, 707);
        var frameRect = new PixelRect(100, 100, 1000, 700);

        // Act
        PixelRect target = WindowRules.AdjustForFrame(new PixelRect(0, 0, 500, 500), windowRect, frameRect, resizable: true);

        // Assert
        await Assert.That(target).IsEqualTo(new PixelRect(-7, 0, 507, 507));
    }

    [Test]
    public async Task AdjustForFrame_FixedSizeWindow_MovesWithoutResizing()
    {
        // Arrange
        var windowRect = new PixelRect(300, 300, 620, 540);

        // Act
        PixelRect target = WindowRules.AdjustForFrame(new PixelRect(0, 0, 960, 1080), windowRect, frameRect: null, resizable: false);

        // Assert
        await Assert.That(target).IsEqualTo(new PixelRect(0, 0, 320, 240));
    }
}
