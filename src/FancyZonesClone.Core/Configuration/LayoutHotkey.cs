namespace FancyZonesClone.Core.Configuration;

/// <summary>A Win+Ctrl+Alt+digit shortcut for a custom layout.</summary>
public sealed class LayoutHotkey
{
    /// <summary>The digit, 0 to 9.</summary>
    public required int Key { get; init; }

    /// <summary>The custom layout it applies.</summary>
    public required Guid LayoutId { get; init; }
}
