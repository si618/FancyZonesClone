using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Diagnostics;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Views;
using FancyZonesClone.Windows;
using Microsoft.Extensions.Logging;

namespace FancyZonesClone;

/// <summary>The tray application: loads the config, runs the zone engine, and serves the tray menu.</summary>
public sealed partial class App : Application, IDisposable
{
    // One log file beside config.json, so a user can attach it to a bug report. The factory disposes its provider.
    private readonly LoggerFactory _loggerFactory = new([new FileLoggerProvider(FileLoggerProvider.DefaultPath, TimeProvider.System)]);
    private readonly ILogger<App> _logger;

    private FancyZonesCloneConfig _config = new();
    private ZoneEngine? _engine;
    private LayoutsWindow? _layoutsWindow;
    private HelpWindow? _helpWindow;
    private AboutWindow? _aboutWindow;
    private bool _reporting;

    /// <summary>Creates the application.</summary>
    public App() => _logger = _loggerFactory.CreateLogger<App>();

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;

        (_config, string source) = ConfigLoader.Load();
        LogConfigLoaded(_logger, source);

        // The engine's hooks call back through this thread's message loop, which Avalonia runs. A layout hotkey
        // already forgets the zones on its own monitor, so the engine only needs the config saved.
        _engine = new ZoneEngine(() => _config, SaveConfig, _loggerFactory);
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
        _loggerFactory.Dispose();
    }

    // A last resort for an exception no handler expected: log it and keep snapping, instead of ending the process.
    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(_logger, e.Exception);
        e.Handled = true;
    }

    // A message box runs a modal loop in which the engine's deferred work, such as a layout hotkey's save, can fail
    // again. Log those failures, but show only one box at a time.
    private void Report(string message)
    {
        LogReported(_logger, message);
        if (_reporting)
        {
            return;
        }

        _reporting = true;
        try
        {
            UserMessages.ShowWarning(message);
        }
        finally
        {
            _reporting = false;
        }
    }

    // An editor or antivirus can hold the file. The running config stays in effect either way.
    private void SaveConfig()
    {
        try
        {
            ConfigStore.Save(_config, ConfigStore.DefaultPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Report($"Couldn't save {ConfigStore.DefaultPath}, so your changes last only until FancyZonesClone exits. {ex.Message}");
        }
    }

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
        PowerToysImportResult result;
        try
        {
            result = ConfigLoader.ImportPowerToys();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Report($"Couldn't read FancyZones' files in {PowerToysFiles.DefaultFolder}, so the current config is unchanged. {ex.Message}");
            return;
        }

        foreach (string warning in result.Warnings)
        {
            LogImportWarning(_logger, warning);
        }

        ReplaceConfig(result.Config, save: true);
    }

    private void OnOpenConfig(object? sender, EventArgs e) => OpenConfigFile();

    private void OpenConfigFile()
    {
        // The file is missing only when every save so far has failed, and a text editor couldn't open it either.
        if (!File.Exists(ConfigStore.DefaultPath))
        {
            Report($"{ConfigStore.DefaultPath} doesn't exist yet, because FancyZonesClone couldn't save it. Check that you can write to its folder.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(ConfigStore.DefaultPath) { UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex)
        {
            // Most often no app is associated with .json.
            Report($"Couldn't open {ConfigStore.DefaultPath}. Open it in a text editor instead. {ex.Message}");
        }
    }

    // Selects the file in File Explorer, which also helps when no app is associated with .json. Explorer parses
    // /select itself, so the path is quoted by hand rather than through ArgumentList.
    private void OpenConfigFolder()
    {
        string path = ConfigStore.DefaultPath;
        string arguments = File.Exists(path) ? $"/select,\"{path}\"" : $"\"{Path.GetDirectoryName(path)}\"";
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = false })?.Dispose();
        }
        catch (Win32Exception ex)
        {
            Report($"Couldn't open the folder of {path}. {ex.Message}");
        }
    }

    private void OnReload(object? sender, EventArgs e)
    {
        // TryLoad doesn't throw, so a file with a typo keeps the running config.
        ConfigLoadResult loaded = ConfigStore.TryLoad(ConfigStore.DefaultPath);
        if (loaded.Error is { } error)
        {
            LogReloadFailed(_logger, ConfigStore.DefaultPath, error);
        }

        if (loaded.Config is { } config)
        {
            ReplaceConfig(config, save: false);
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        if (_aboutWindow is { } open)
        {
            open.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        _aboutWindow.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow.Show();
    }

    // The window reads the config when it opens, so a reload while it's open shows on the next open.
    private void OnHelp(object? sender, EventArgs e)
    {
        if (_helpWindow is { } open)
        {
            open.Activate();
            return;
        }

        _helpWindow = new HelpWindow(_config, ConfigStore.DefaultPath, OpenConfigFile, OpenConfigFolder);
        _helpWindow.Closed += (_, _) => _helpWindow = null;
        _helpWindow.Show();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "Config: {Source}")]
    private static partial void LogConfigLoaded(ILogger logger, string source);

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "An unhandled exception reached the dispatcher.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "{Message}")]
    private static partial void LogReported(ILogger logger, string message);

    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "PowerToys import: {Warning}")]
    private static partial void LogImportWarning(ILogger logger, string warning);

    [LoggerMessage(EventId = 104, Level = LogLevel.Warning, Message = "Couldn't reload {Path}; keeping the current config. {Error}")]
    private static partial void LogReloadFailed(ILogger logger, string path, string error);
}
