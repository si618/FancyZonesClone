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
        // Win32 only, rather than UsePlatformDetect, so Native AOT doesn't compile in the X11 and macOS backends.
        .UseWin32()
        .UseSkia()
        .UseHarfBuzz()
        // Software rendering, so the app never loads ANGLE and the publish output doesn't ship av_libglesv2.dll.
        // The tray menu and the layouts window are too light to need the GPU.
        .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Software] })
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
