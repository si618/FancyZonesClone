using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Snapping;

/// <summary>
/// The drag-to-zone state machine. The Windows layer reports a window move starting, the cursor and modifiers
/// while it moves, and the move ending; the controller drives the overlay and decides where the window lands.
/// </summary>
public sealed class DragController(IZoneSetSource zoneSets, IZoneOverlay overlay, Func<FancyZonesCloneSettings> settings)
{
    private readonly ZoneHighlighter _highlighter = new();
    private nint _window;
    private MonitorDescriptor? _monitor;
    private ZoneSet _zoneSet = ZoneSet.Empty;
    private bool _overlayVisible;

    /// <summary>Whether a window move is in progress.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>Starts tracking a move of <paramref name="window"/>. The caller filters out windows it can't snap.</summary>
    public void Begin(nint window)
    {
        Cancel();
        _window = window;
        IsDragging = true;
    }

    /// <summary>Updates the overlay for the cursor at <paramref name="cursor"/>, on <paramref name="monitorUnderCursor"/>.</summary>
    public void Update(PixelPoint cursor, DragModifiers modifiers, MonitorDescriptor? monitorUnderCursor)
    {
        if (!IsDragging)
        {
            return;
        }

        FancyZonesCloneSettings current = settings();
        bool zonesActive = current.ShiftDrag ? modifiers.Shift : !modifiers.Shift;
        if (!zonesActive || monitorUnderCursor is null)
        {
            HideOverlay();
            return;
        }

        bool monitorChanged = _monitor is null || _monitor.Handle != monitorUnderCursor.Handle;
        if (monitorChanged)
        {
            _monitor = monitorUnderCursor;
            _zoneSet = zoneSets.GetZoneSet(monitorUnderCursor);
            _highlighter.Reset();
        }

        if (_zoneSet.IsEmpty)
        {
            HideOverlay();
            return;
        }

        bool highlightChanged = _highlighter.Update(
            _zoneSet, monitorUnderCursor.ToWorkArea(cursor), modifiers.Ctrl, current.OverlappingZonesAlgorithm);

        if (highlightChanged || monitorChanged || !_overlayVisible)
        {
            overlay.Show(monitorUnderCursor, _zoneSet, _highlighter.Zones);
            _overlayVisible = true;
        }
    }

    /// <summary>Ends the move. Returns where to place the window, or null to leave it where the user dropped it.</summary>
    public SnapRequest? End()
    {
        if (!IsDragging)
        {
            return null;
        }

        SnapRequest? request = null;
        if (_overlayVisible && _monitor is not null && _zoneSet.CombinedRect(_highlighter.Zones) is { } rect)
        {
            request = new SnapRequest(_window, _monitor, _highlighter.Zones, _monitor.ToScreen(rect));
        }

        Cancel();
        return request;
    }

    /// <summary>
    /// Shows <paramref name="zoneSet"/> on <paramref name="monitor"/> with nothing highlighted, such as after a
    /// layout hotkey. Does nothing during a move. A move that starts while the zones show hides them, so the
    /// overlay can't outlive both the flash and the move.
    /// </summary>
    /// <returns>Whether the zones are now showing; call <see cref="EndFlash"/> to hide them.</returns>
    public bool Flash(MonitorDescriptor monitor, ZoneSet zoneSet)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(zoneSet);

        if (IsDragging || zoneSet.IsEmpty)
        {
            return false;
        }

        overlay.Show(monitor, zoneSet, []);
        _overlayVisible = true;
        return true;
    }

    /// <summary>Hides the zones <see cref="Flash"/> showed, unless a move has since taken the overlay over.</summary>
    public void EndFlash()
    {
        if (!IsDragging)
        {
            HideOverlay();
        }
    }

    /// <summary>Abandons the move and hides the overlay.</summary>
    public void Cancel()
    {
        HideOverlay();
        IsDragging = false;
        _window = 0;
        _monitor = null;
        _zoneSet = ZoneSet.Empty;
    }

    private void HideOverlay()
    {
        _highlighter.Reset();
        if (_overlayVisible)
        {
            overlay.Hide();
            _overlayVisible = false;
        }
    }
}
