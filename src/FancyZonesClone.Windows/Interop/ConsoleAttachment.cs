namespace FancyZonesClone.Windows.Interop;

/// <summary>Lets a GUI executable write to the console that started it.</summary>
public static class ConsoleAttachment
{
    private const int AttachParentProcess = -1;

    /// <summary>Attaches to the parent's console. Returns false when started without one, such as from Explorer.</summary>
    public static bool AttachToParent() => Native.AttachConsole(AttachParentProcess);
}
