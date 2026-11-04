using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Snapping;

namespace FancyZonesClone.Core.Tests.Snapping;

public class DragControllerTests
{
    private const nint Window = 0x1234;

    // A secondary monitor to the right of a 1920-wide primary, with a 40-pixel taskbar at the top.
    private static readonly MonitorDescriptor _right = new(
        2, new MonitorIdentity("DEL4233", "4&1&0&UID2"), new PixelRect(1920, 0, 3840, 1080), new PixelRect(1920, 40, 3840, 1080), 2);

    private static readonly MonitorDescriptor _left = new(
        1, new MonitorIdentity("GSM7863", "4&1&0&UID1"), new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1080), 1);

    [Test]
    public async Task End_ShiftHeldOverSecondColumn_SnapsToThatColumnInScreenCoordinates()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);
        controller.Update(new PixelPoint(1920 + 960, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Act
        SnapRequest? request = controller.End();

        // Assert
        await Assert.That(request).IsNotNull();
        await Assert.That(request!.ZoneIndexes).IsEquivalentTo([1], CollectionOrdering.Matching);
        await Assert.That(request.ScreenRect).IsEqualTo(new PixelRect(1920 + 640, 40, 1920 + 1280, 1080));
        await Assert.That(request.Window).IsEqualTo(Window);
    }

    [Test]
    public async Task End_ShiftNotHeld_LeavesWindowWhereDropped()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: false, Ctrl: false), _right);

        // Act
        SnapRequest? request = controller.End();

        // Assert
        await Assert.That(request).IsNull();
        await Assert.That(overlay.ShowCount).IsEqualTo(0);
    }

    [Test]
    public async Task End_ShiftDragOffAndShiftHeld_SuppressesZones()
    {
        // Arrange
        DragController controller = CreateController(new RecordingOverlay(), new FancyZonesCloneSettings { ShiftDrag = false });
        controller.Begin(Window);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Act
        SnapRequest? request = controller.End();

        // Assert
        await Assert.That(request).IsNull();
    }

    [Test]
    public async Task End_ShiftReleasedBeforeDrop_HidesOverlayAndDoesNotSnap()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: true, Ctrl: false), _right);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: false, Ctrl: false), _right);

        // Act
        SnapRequest? request = controller.End();

        // Assert
        await Assert.That(request).IsNull();
        await Assert.That(overlay.IsVisible).IsFalse();
    }

    [Test]
    public async Task End_CtrlHeldWhileMovingAcrossColumns_SpansFromFirstToLastColumn()
    {
        // Arrange
        DragController controller = CreateController(new RecordingOverlay(), new FancyZonesCloneSettings());
        controller.Begin(Window);
        controller.Update(new PixelPoint(1920 + 100, 500), new DragModifiers(Shift: true, Ctrl: true), _right);
        controller.Update(new PixelPoint(1920 + 1800, 500), new DragModifiers(Shift: true, Ctrl: true), _right);

        // Act
        SnapRequest? request = controller.End();

        // Assert
        await Assert.That(request!.ZoneIndexes).IsEquivalentTo([0, 1, 2], CollectionOrdering.Matching);
        await Assert.That(request.ScreenRect).IsEqualTo(new PixelRect(1920, 40, 3840, 1080));
    }

    [Test]
    public async Task Update_HighlightUnchanged_DoesNotRedrawOverlay()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);

        // Act
        controller.Update(new PixelPoint(1920 + 100, 500), new DragModifiers(Shift: true, Ctrl: false), _right);
        controller.Update(new PixelPoint(1920 + 110, 510), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Assert
        await Assert.That(overlay.ShowCount).IsEqualTo(1);
    }

    [Test]
    public async Task Update_CursorMovesToAnotherMonitor_ShowsThatMonitorsZones()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);
        controller.Update(new PixelPoint(2000, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Act
        controller.Update(new PixelPoint(100, 500), new DragModifiers(Shift: true, Ctrl: false), _left);

        // Assert
        await Assert.That(overlay.LastMonitor).IsEqualTo(_left);
        await Assert.That(controller.End()!.Monitor).IsEqualTo(_left);
    }

    [Test]
    public async Task Update_BlankLayoutOnMonitor_NeverShowsOverlay()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        var controller = new DragController(new FixedZoneSets(ZoneSet.Empty), overlay, () => new FancyZonesCloneSettings());
        controller.Begin(Window);

        // Act
        controller.Update(new PixelPoint(2000, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Assert
        await Assert.That(overlay.ShowCount).IsEqualTo(0);
        await Assert.That(controller.End()).IsNull();
    }

    [Test]
    public async Task Update_NotDragging_IsIgnored()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());

        // Act
        controller.Update(new PixelPoint(2000, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Assert
        await Assert.That(overlay.ShowCount).IsEqualTo(0);
    }

    [Test]
    public async Task Begin_DuringFlash_HidesTheFlashedZones()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Flash(_right, new ColumnsPerMonitor().GetZoneSet(_right));

        // Act
        controller.Begin(Window);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: false, Ctrl: false), _right);
        controller.EndFlash();

        // Assert
        await Assert.That(overlay.IsVisible).IsFalse();
    }

    [Test]
    public async Task EndFlash_MoveShowingZones_LeavesTheOverlayToTheMove()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Flash(_right, new ColumnsPerMonitor().GetZoneSet(_right));
        controller.Begin(Window);
        controller.Update(new PixelPoint(2880, 500), new DragModifiers(Shift: true, Ctrl: false), _right);

        // Act
        controller.EndFlash();

        // Assert
        await Assert.That(overlay.IsVisible).IsTrue();
        await Assert.That(controller.End()).IsNotNull();
    }

    [Test]
    public async Task EndFlash_NoMove_HidesTheFlashedZones()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        bool shown = controller.Flash(_right, new ColumnsPerMonitor().GetZoneSet(_right));

        // Act
        controller.EndFlash();

        // Assert
        await Assert.That(shown).IsTrue();
        await Assert.That(overlay.IsVisible).IsFalse();
    }

    [Test]
    public async Task Flash_DuringMove_DoesNotShowZones()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        DragController controller = CreateController(overlay, new FancyZonesCloneSettings());
        controller.Begin(Window);

        // Act
        bool shown = controller.Flash(_right, new ColumnsPerMonitor().GetZoneSet(_right));

        // Assert
        await Assert.That(shown).IsFalse();
        await Assert.That(overlay.ShowCount).IsEqualTo(0);
    }

    private static DragController CreateController(RecordingOverlay overlay, FancyZonesCloneSettings settings) =>
        new(new ColumnsPerMonitor(), overlay, () => settings);

    private sealed class ColumnsPerMonitor : IZoneSetSource
    {
        public ZoneSet GetZoneSet(MonitorDescriptor monitor) =>
            new(ZoneCalculator.Columns(monitor.WorkArea.Width, monitor.WorkArea.Height, 3, spacing: 0), sensitivityRadius: 20);
    }

    private sealed class FixedZoneSets(ZoneSet zoneSet) : IZoneSetSource
    {
        public ZoneSet GetZoneSet(MonitorDescriptor monitor) => zoneSet;
    }

    private sealed class RecordingOverlay : IZoneOverlay
    {
        public int ShowCount { get; private set; }

        public bool IsVisible { get; private set; }

        public MonitorDescriptor? LastMonitor { get; private set; }

        public void Show(MonitorDescriptor monitor, ZoneSet zoneSet, IReadOnlyList<int> highlighted)
        {
            ShowCount++;
            IsVisible = true;
            LastMonitor = monitor;
        }

        public void Hide() => IsVisible = false;
    }
}
