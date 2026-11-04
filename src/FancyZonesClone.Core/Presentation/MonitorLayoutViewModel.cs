using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Presentation;

/// <summary>One monitor in the layouts window: its name, its layout picker, and a preview of the zones.</summary>
public sealed class MonitorLayoutViewModel : INotifyPropertyChanged
{
    private readonly FancyZonesCloneConfig _config;
    private readonly MonitorDescriptor _monitor;
    private readonly Action _changed;

    /// <summary>Creates the row, selecting the monitor's current layout.</summary>
    /// <param name="config">The configuration to read and update.</param>
    /// <param name="monitor">The monitor.</param>
    /// <param name="changed">Called after the user picks another layout, to save and apply it.</param>
    public MonitorLayoutViewModel(FancyZonesCloneConfig config, MonitorDescriptor monitor, Action changed)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(monitor);

        _config = config;
        _monitor = monitor;
        _changed = changed;

        LayoutSpec current = LayoutResolver.Resolve(config, monitor);
        var choices = LayoutChoice.All(config).ToList();
        LayoutChoice? selected = choices.FirstOrDefault(c => c.Matches(current));
        if (selected is null)
        {
            selected = LayoutChoice.ForCurrent(current);
            choices.Insert(0, selected);
        }

        Choices = choices;
        SelectedChoice = selected;
        Zones = CalculateZones();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The monitor's display number, device ID, and resolution, as shown in the window.</summary>
    public string Title => string.Create(
        CultureInfo.InvariantCulture,
        $"Display {_monitor.Number}: {_monitor.Identity.DeviceId} ({_monitor.MonitorArea.Width} × {_monitor.MonitorArea.Height})");

    /// <summary>The layouts the user can pick.</summary>
    public IReadOnlyList<LayoutChoice> Choices { get; }

    /// <summary>The picked layout. Setting it assigns the layout to the monitor and refreshes the preview.</summary>
    public LayoutChoice SelectedChoice
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(field, value))
            {
                return;
            }

            bool initialising = field is null;
            field = value;
            OnPropertyChanged();

            if (!initialising)
            {
                LayoutResolver.Assign(_config, _monitor.Identity, value.CreateLayout());
                Zones = CalculateZones();
                _changed();
            }
        }
    }

    /// <summary>The zones of the picked layout, relative to the work area, with the indexes the overlay numbers them by.</summary>
    public IReadOnlyList<Zone> Zones
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The work area's width, which the preview scales from.</summary>
    public int WorkAreaWidth => _monitor.WorkArea.Width;

    /// <summary>The work area's height, which the preview scales from.</summary>
    public int WorkAreaHeight => _monitor.WorkArea.Height;

    private IReadOnlyList<Zone> CalculateZones() => LayoutResolver.ZoneSetFor(_config, _monitor).Zones;

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
