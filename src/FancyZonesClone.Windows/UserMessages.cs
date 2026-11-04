using FancyZonesClone.Windows.Interop;

namespace FancyZonesClone.Windows;

/// <summary>Tells the user about a failure they need to act on, since a tray app has no window to show it in.</summary>
public static class UserMessages
{
    /// <summary>
    /// Shows <paramref name="text"/> in a warning message box and waits for it to close. The box runs its own message
    /// loop, so the hooks keep running meanwhile.
    /// </summary>
    public static void ShowWarning(string text) =>
        _ = Native.MessageBoxW(0, text, "FancyZonesClone", Native.MB_ICONWARNING | Native.MB_SETFOREGROUND);
}
