using Avalonia.Controls;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Views;

/// <summary>Lists the connected monitors, each with a layout picker and a zone preview.</summary>
public sealed partial class LayoutsWindow : Window
{
    /// <summary>Required by the XAML designer; the app uses the other constructor.</summary>
    public LayoutsWindow()
        : this(new FancyZonesCloneConfig(), [], _ => { })
    {
    }

    /// <summary>Creates the window for the given monitors.</summary>
    /// <param name="config">The configuration to read and update.</param>
    /// <param name="monitors">The connected monitors.</param>
    /// <param name="changed">Called with the monitor whose layout the user changed, to save and apply it.</param>
    public LayoutsWindow(FancyZonesCloneConfig config, IReadOnlyList<MonitorDescriptor> monitors, Action<MonitorDescriptor> changed)
    {
        Rows = [.. monitors.OrderBy(m => m.Number).Select(m => new MonitorLayoutViewModel(config, m, () => changed(m)))];
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>One row per monitor, by display number.</summary>
    public IReadOnlyList<MonitorLayoutViewModel> Rows { get; }
}
