using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Views;
using FancyZonesClone.Windows;

namespace FancyZonesClone;

/// <summary>The tray application: loads the config, runs the zone engine, and serves the tray menu.</summary>
public sealed class App : Application, IDisposable
{
    private FancyZonesCloneConfig _config = new();
    private ZoneEngine? _engine;
    private LayoutsWindow? _layoutsWindow;

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        (_config, string source) = ConfigLoader.Load();
        Trace.TraceInformation($"FancyZonesClone config: {source}");

        // The engine's hooks call back through this thread's message loop, which Avalonia runs. A layout hotkey
        // already forgets the zones on its own monitor, so the engine only needs the config saved.
        _engine = new ZoneEngine(() => _config, SaveConfig);
        _engine.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) => Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }

    private void SaveConfig() => ConfigStore.Save(_config, ConfigStore.DefaultPath);

    // Only the changed monitor's windows lose their zones; windows snapped elsewhere keep moving zone to zone.
    private void OnMonitorLayoutChanged(MonitorDescriptor monitor)
    {
        SaveConfig();
        _engine?.ResetAssignments(monitor.Handle);
    }

    private void ShowLayouts()
    {
        if (_layoutsWindow is { } open)
        {
            open.Activate();
            return;
        }

        _layoutsWindow = new LayoutsWindow(_config, Monitors.GetAll(), OnMonitorLayoutChanged);
        _layoutsWindow.Closed += (_, _) => _layoutsWindow = null;
        _layoutsWindow.Show();
    }

    // Swaps in a new config. Every window loses its zones, because the layouts may have changed under them. A
    // config read from the file is already saved, so a reload skips the save.
    private void ReplaceConfig(FancyZonesCloneConfig config, bool save)
    {
        _config = config;
        _layoutsWindow?.Close();
        if (save)
        {
            SaveConfig();
        }

        _engine?.ResetAssignments();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowLayouts();

    private void OnLayouts(object? sender, EventArgs e) => ShowLayouts();

    private void OnPause(object? sender, EventArgs e)
    {
        // The engine's state is the truth, whether or not the platform menu toggled the check mark itself.
        if (_engine is not null && sender is NativeMenuItem item)
        {
            _engine.Paused = !_engine.Paused;
            item.IsChecked = _engine.Paused;
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        PowerToysImportResult result = ConfigLoader.ImportPowerToys();
        foreach (string warning in result.Warnings)
        {
            Trace.TraceWarning($"FancyZonesClone import: {warning}");
        }

        ReplaceConfig(result.Config, save: true);
    }

    private void OnOpenConfig(object? sender, EventArgs e) =>
        Process.Start(new ProcessStartInfo(ConfigStore.DefaultPath) { UseShellExecute = true })?.Dispose();

    private void OnReload(object? sender, EventArgs e)
    {
        // TryLoad doesn't throw, so a file with a typo keeps the running config.
        ConfigLoadResult loaded = ConfigStore.TryLoad(ConfigStore.DefaultPath);
        if (loaded.Error is { } error)
        {
            Trace.TraceWarning($"FancyZonesClone: couldn't reload {ConfigStore.DefaultPath}; keeping the current config. {error}");
        }

        if (loaded.Config is { } config)
        {
            ReplaceConfig(config, save: false);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
