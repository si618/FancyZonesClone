using Avalonia;
using FancyZonesClone;
using FancyZonesClone.Diagnostics;

// Diagnose installs no hooks, so it runs beside a tray app that's already up.
if (args.Contains("--diagnose", StringComparer.OrdinalIgnoreCase))
{
    return Diagnose.Run();
}

// One instance per user session: two engines would both snap every window.
using var singleInstance = new Mutex(initiallyOwned: true, @"Local\FancyZonesClone", out bool isFirstInstance);
return !isFirstInstance
    ? 0
    : AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
