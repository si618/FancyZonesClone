using Avalonia.Controls;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Views;

/// <summary>Explains snapping, hotkeys, the tray menu, and the config file, in the system's light or dark theme.</summary>
public sealed partial class HelpWindow : Window
{
    /// <summary>Required by the XAML designer; the app uses the other constructor.</summary>
    public HelpWindow()
        : this(new FancyZonesCloneConfig(), ConfigStore.DefaultPath)
    {
    }

    /// <summary>Creates the window for the running configuration.</summary>
    /// <param name="config">The configuration whose settings the text describes.</param>
    /// <param name="configPath">Where the config file lives.</param>
    public HelpWindow(FancyZonesCloneConfig config, string configPath)
    {
        Sections = HelpText.Create(config, configPath);
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>The line above the sections.</summary>
    public string Intro { get; } = HelpText.Intro;

    /// <summary>The headed lists, in display order.</summary>
    public IReadOnlyList<HelpSection> Sections { get; }
}
