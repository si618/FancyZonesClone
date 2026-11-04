using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Snapping;
using FancyZonesClone.Core.Windows;
using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>
/// Connects Windows to the Core state machines: WinEvent hooks report window moves, a timer samples the cursor and
/// modifiers while a move is in progress, and a low-level keyboard hook handles Win+arrow and layout hotkeys.
/// </summary>
/// <remarks>
/// Hooks and timers call back on the thread that installs them, through its message loop, so <see cref="Start"/>
/// must run on a thread that pumps messages, such as the Avalonia UI thread. Only one engine runs per process.
/// </remarks>
public sealed unsafe class ZoneEngine : IZoneSetSource, IDisposable
{
    // Cursor sampling interval during a drag: about 100 Hz, smoother than a 60 Hz display needs.
    private const uint DragSampleMilliseconds = 10;

    // How long the zones flash after a layout hotkey switches the layout.
    private const uint FlashMilliseconds = 600;

    private static ZoneEngine? _instance;

    private readonly Func<FancyZonesCloneConfig> _config;
    private readonly Action _configChanged;
    private readonly OverlayWindow _overlay;
    private readonly DragController _drag;
    private readonly ZoneAssignments _assignments = new();
    private readonly MonitorZoneCache _monitors = new();
    private readonly Queue<Action> _deferred = new();

    private nint _moveSizeHook;
    private nint _destroyHook;
    private nint _foregroundHook;
    private nint _keyboardHook;
    private nuint _dragTimer;
    private nuint _deferTimer;
    private nuint _flashTimer;
    private nint _dragWindow;

    // The monitor under the cursor at the last drag sample, so a sample that stays on it skips describing it again.
    private MonitorDescriptor? _dragMonitor;

    // The foreground window and its executable path, read when it came to the foreground, so the keyboard hook
    // doesn't open its process.
    private nint _foregroundWindow;
    private string _foregroundProcessPath = "";

    /// <summary>Creates the engine.</summary>
    /// <param name="config">Returns the current configuration; read on every event, so edits apply at once.</param>
    /// <param name="configChanged">Called to save the configuration after the engine changes it, such as for a layout hotkey.</param>
    public ZoneEngine(Func<FancyZonesCloneConfig> config, Action configChanged)
    {
        _config = config;
        _configChanged = configChanged;
        _overlay = new OverlayWindow(() => _config().Settings);
        _drag = new DragController(this, _overlay, () => _config().Settings);
    }

    /// <summary>When set, windows move freely and the keyboard shortcuts pass through to Windows.</summary>
    public bool Paused { get; set; }

    /// <summary>Installs the hooks. Call on a thread with a message loop.</summary>
    public void Start()
    {
        if (_instance is not null)
        {
            throw new InvalidOperationException("A ZoneEngine is already running in this process.");
        }

        _instance = this;
        const uint Flags = Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS;
        _moveSizeHook = Native.SetWinEventHook(Native.EVENT_SYSTEM_MOVESIZESTART, Native.EVENT_SYSTEM_MOVESIZEEND, 0, &OnWinEvent, 0, 0, Flags);
        _destroyHook = Native.SetWinEventHook(Native.EVENT_OBJECT_DESTROY, Native.EVENT_OBJECT_DESTROY, 0, &OnWinEvent, 0, 0, Flags);

        // Not skipping this process: Win+arrow can move the layouts window too, so its path must be cached as well.
        _foregroundHook = Native.SetWinEventHook(
            Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND, 0, &OnWinEvent, 0, 0, Native.WINEVENT_OUTOFCONTEXT);
        _keyboardHook = Native.SetWindowsHookExW(Native.WH_KEYBOARD_LL, &OnKeyboard, Native.GetModuleHandleW(lpModuleName: null), 0);

        if (_moveSizeHook == 0 || _destroyHook == 0 || _foregroundHook == 0 || _keyboardHook == 0)
        {
            Dispose();
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Couldn't install the window hooks (Win32 error {Marshal.GetLastPInvokeError()})."));
        }

        RefreshMonitors();
        TrackForeground(Native.GetForegroundWindow());
    }

    /// <inheritdoc/>
    public ZoneSet GetZoneSet(MonitorDescriptor monitor) => LayoutResolver.ZoneSetFor(_config(), monitor);

    /// <summary>
    /// Forgets which zones windows occupy and rereads every monitor's layout, such as after the configuration is
    /// reloaded.
    /// </summary>
    public void ResetAssignments()
    {
        _assignments.Clear();
        RefreshMonitors();
    }

    /// <summary>
    /// Forgets which zones windows on <paramref name="monitor"/> occupy and rereads every monitor's layout, such as
    /// after its layout changes.
    /// </summary>
    public void ResetAssignments(nint monitor)
    {
        _assignments.RemoveMonitor(monitor);
        RefreshMonitors();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopTimer(ref _dragTimer);
        StopTimer(ref _deferTimer);
        StopTimer(ref _flashTimer);
        Unhook(ref _moveSizeHook);
        Unhook(ref _destroyHook);
        Unhook(ref _foregroundHook);
        if (_keyboardHook != 0)
        {
            Native.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = 0;
        }

        _drag.Cancel();
        _overlay.Dispose();
        if (ReferenceEquals(_instance, this))
        {
            _instance = null;
        }
    }

    private static PixelPoint CursorPosition() =>
        Native.GetCursorPos(out POINT point) ? new PixelPoint(point.X, point.Y) : default;

    private static DragModifiers CurrentModifiers() =>
        new(Native.IsKeyDown(Native.VK_SHIFT), Native.IsKeyDown(Native.VK_CONTROL));

    private static void StopTimer(ref nuint timer)
    {
        if (timer != 0)
        {
            Native.KillTimer(0, timer);
            timer = 0;
        }
    }

    private static void Unhook(ref nint hook)
    {
        if (hook != 0)
        {
            Native.UnhookWinEvent(hook);
            hook = 0;
        }
    }

    private bool IsSnappable(nint window) => IsSnappable(WindowOperations.ReadTraits(window));

    private bool IsSnappable(WindowTraits traits)
    {
        FancyZonesCloneSettings settings = _config().Settings;
        return WindowRules.Classify(traits, settings.AllowChildWindowSnap, settings.ExcludedApps) == Processability.Processable;
    }

    // Describes every monitor and builds its zone set, outside the keyboard hook, which then only looks them up. It
    // runs from Start and the app's handlers as well as from Guard, so it traps its own exceptions: a layout that
    // can't be built leaves the cache empty, and Win+arrow passes through to Windows.
#pragma warning disable CA1031 // Catch everything: an escaping exception would stop the app starting or end it.
    private void RefreshMonitors()
    {
        try
        {
            _monitors.Refresh(Monitors.GetAll(), this);
        }
        catch (Exception ex)
        {
            _monitors.Clear();
            Trace.TraceError($"FancyZonesClone: couldn't read the monitors' layouts: {ex}");
        }
    }
#pragma warning restore CA1031

    private void TrackForeground(nint window)
    {
        _foregroundWindow = window;
        _foregroundProcessPath = window != 0 ? WindowOperations.ProcessPath(window) : "";
    }

    private void OnMoveSizeStart(nint window)
    {
        if (Paused || WindowOperations.IsResizing(window, CursorPosition()) || !IsSnappable(window))
        {
            return;
        }

        // A start with no end for the previous move, such as when its window closed mid-move, replaces that move.
        StopTimer(ref _dragTimer);
        _dragWindow = window;
        _dragMonitor = null;
        _drag.Begin(window);
        SampleDrag();
        _dragTimer = Native.SetTimer(0, 0, DragSampleMilliseconds, &OnDragTimer);
    }

    private void OnMoveSizeEnd(nint window)
    {
        if (window != _dragWindow)
        {
            return;
        }

        StopDragging();

        if (_drag.End() is { } request)
        {
            Snap(request);
        }
        else
        {
            _assignments.Remove(window);
        }
    }

    private void OnWindowDestroyed(nint window)
    {
        _assignments.Remove(window);
        if (window == _dragWindow)
        {
            StopDragging();
            _drag.Cancel();
        }
    }

    private void StopDragging()
    {
        StopTimer(ref _dragTimer);
        _dragWindow = 0;
        _dragMonitor = null;
    }

    private void SampleDrag()
    {
        PixelPoint cursor = CursorPosition();
        nint monitor = Monitors.HandleFromPoint(cursor);
        if (_dragMonitor?.Handle != monitor)
        {
            _dragMonitor = Monitors.Describe(monitor);
        }

        _drag.Update(cursor, CurrentModifiers(), _dragMonitor);
    }

    private void Snap(SnapRequest request)
    {
        if (WindowOperations.MoveToZone(request.Window, request.ScreenRect, request.Monitor))
        {
            _assignments.Assign(request.Window, request.Monitor.Handle, request.ZoneIndexes);
        }
    }

    private bool HandleKey(uint virtualKey)
    {
        FancyZonesCloneSettings settings = _config().Settings;
        bool win = Native.IsKeyDown(Native.VK_LWIN) || Native.IsKeyDown(Native.VK_RWIN);
        bool ctrl = Native.IsKeyDown(Native.VK_CONTROL);
        bool alt = Native.IsKeyDown(Native.VK_MENU);
        bool shift = Native.IsKeyDown(Native.VK_SHIFT);

        if (Paused || !win || shift)
        {
            return false;
        }

        if (!ctrl && !alt && settings.OverrideSnapHotkeys && virtualKey is Native.VK_LEFT or Native.VK_RIGHT)
        {
            return TakeSnapHotkey(virtualKey == Native.VK_LEFT ? HorizontalDirection.Left : HorizontalDirection.Right);
        }

        if (ctrl && alt && settings.QuickLayoutSwitch && DigitOf(virtualKey) is { } digit &&
            _config().LayoutHotkeys.FirstOrDefault(h => h.Key == digit) is { } hotkey)
        {
            // A hotkey left behind after its layout was deleted would otherwise replace a working layout with no zones.
            if (LayoutResolver.FindCustomLayout(_config(), hotkey.LayoutId) is null)
            {
                Trace.TraceWarning(string.Create(CultureInfo.InvariantCulture, $"FancyZonesClone: layout hotkey {digit} points at missing custom layout {hotkey.LayoutId}; ignoring it."));
                return false;
            }

            Defer(() => ApplyLayout(hotkey.LayoutId));
            return true;
        }

        return false;
    }

    // As FancyZones' ShouldProcessSnapHotkey does, take Win+arrow only when there are zones to move to. Otherwise it
    // passes through and Windows' own snap runs. Windows removes a low-level hook that misses LowLevelHooksTimeout,
    // so this reads only cached facts and cheap window and monitor state. On a cache miss, the key passes through and
    // the cache is refreshed for the next press.
    private bool TakeSnapHotkey(HorizontalDirection direction)
    {
        nint window = Native.GetForegroundWindow();
        if (window == 0)
        {
            return false;
        }

        if (window != _foregroundWindow)
        {
            Defer(() => TrackForeground(window));
            return false;
        }

        if (!IsSnappable(WindowOperations.ReadTraits(window, _foregroundProcessPath)))
        {
            return false;
        }

        nint handle = Monitors.HandleFromWindow(window);
        if (!Monitors.TryGetAreas(handle, out PixelRect monitorArea, out PixelRect workArea))
        {
            return false;
        }

        if (_monitors.Find(handle, monitorArea, workArea, out bool hasZones) is not { } monitor)
        {
            Defer(RefreshMonitors);
            return false;
        }

        if (!hasZones)
        {
            return false;
        }

        Defer(() => MoveByKeyboard(window, monitor, direction));
        return true;
    }

    private static int? DigitOf(uint virtualKey) => virtualKey switch
    {
        >= '0' and <= '9' => (int)(virtualKey - '0'),
        >= Native.VK_NUMPAD0 and <= Native.VK_NUMPAD0 + 9 => (int)(virtualKey - Native.VK_NUMPAD0),
        _ => null,
    };

    private void MoveByKeyboard(nint window, MonitorDescriptor monitor, HorizontalDirection direction)
    {
        if (KeyboardSnapper.Plan(window, monitor, GetZoneSet(monitor), _assignments, direction) is { } request)
        {
            Snap(request);
        }
    }

    private void ApplyLayout(Guid customLayoutId)
    {
        nint foreground = Native.GetForegroundWindow();
        MonitorDescriptor? monitor = (foreground != 0 ? Monitors.FromWindow(foreground) : null) ?? Monitors.FromPoint(CursorPosition());
        if (monitor is null)
        {
            return;
        }

        LayoutResolver.Assign(_config(), monitor.Identity, LayoutSpec.ForCustom(customLayoutId));
        _assignments.RemoveMonitor(monitor.Handle);
        RefreshMonitors();
        _configChanged();

        // Flash the new zones, as FancyZones does on a quick layout switch.
        if (_drag.Flash(monitor, GetZoneSet(monitor)))
        {
            StopTimer(ref _flashTimer);
            _flashTimer = Native.SetTimer(0, 0, FlashMilliseconds, &OnFlashTimer);
        }
    }

    // Work triggered by the keyboard hook runs after the hook returns: Windows abandons a low-level hook that's slow
    // to answer, and moving another process's window can be.
    private void Defer(Action action)
    {
        _deferred.Enqueue(action);
        if (_deferTimer == 0)
        {
            _deferTimer = Native.SetTimer(0, 0, 0, &OnDeferTimer);
        }
    }

    // Each callback traps and traces its exceptions, because one escaping an unmanaged callback terminates the process.
#pragma warning disable CA1031 // Catch everything: any exception that escapes would end the process mid-drag.
    private static void Guard(Action<ZoneEngine> action)
    {
        if (_instance is not { } engine)
        {
            return;
        }

        try
        {
            action(engine);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"FancyZonesClone: {ex}");
        }
    }
#pragma warning restore CA1031

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnWinEvent(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time)
    {
        if (window == 0 || objectId != Native.OBJID_WINDOW || childId != Native.CHILDID_SELF)
        {
            return;
        }

        Guard(engine =>
        {
            switch (eventType)
            {
                case Native.EVENT_SYSTEM_MOVESIZESTART:
                    engine.OnMoveSizeStart(window);
                    break;
                case Native.EVENT_SYSTEM_MOVESIZEEND:
                    engine.OnMoveSizeEnd(window);
                    break;
                case Native.EVENT_OBJECT_DESTROY:
                    engine.OnWindowDestroyed(window);
                    break;
                case Native.EVENT_SYSTEM_FOREGROUND:
                    engine.TrackForeground(window);
                    break;
                default:
                    break;
            }
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint OnKeyboard(int code, nint message, nint data)
    {
        if (code == Native.HC_ACTION && (uint)message is Native.WM_KEYDOWN or Native.WM_SYSKEYDOWN)
        {
            var key = (KBDLLHOOKSTRUCT*)data;
            uint virtualKey = key->vkCode;
            bool handled = false;

            // Every keystroke on the system passes through here, so reject the common case before allocating the
            // closure: only a non-injected key with Win held, while an unpaused engine runs, can be a shortcut.
            if ((key->flags & Native.LLKHF_INJECTED) == 0 &&
                _instance is { Paused: false } &&
                (Native.IsKeyDown(Native.VK_LWIN) || Native.IsKeyDown(Native.VK_RWIN)))
            {
                Guard(engine => handled = engine.HandleKey(virtualKey));
            }

            if (handled)
            {
                WindowOperations.SuppressStartMenu();
                return 1;
            }
        }

        return Native.CallNextHookEx(0, code, message, data);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnDragTimer(nint window, uint message, nuint id, uint time) =>
        Guard(engine =>
        {
            // A timer that isn't the current drag's is a leftover: stop it rather than let it tick forever.
            if (id != engine._dragTimer)
            {
                Native.KillTimer(0, id);
            }
            else if (engine._drag.IsDragging)
            {
                engine.SampleDrag();
            }
            else
            {
                engine.StopDragging();
            }
        });

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnDeferTimer(nint window, uint message, nuint id, uint time) =>
        Guard(engine =>
        {
            StopTimer(ref engine._deferTimer);
            while (engine._deferred.TryDequeue(out Action? action))
            {
                action();
            }
        });

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnFlashTimer(nint window, uint message, nuint id, uint time) =>
        Guard(engine =>
        {
            StopTimer(ref engine._flashTimer);
            engine._drag.EndFlash();
        });
}
