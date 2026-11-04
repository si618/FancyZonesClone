using System.Globalization;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Windows;

namespace FancyZonesClone.Diagnostics;

/// <summary>
/// <c>FancyZonesClone.exe --diagnose</c>: prints each monitor's identity, the layout it resolves to, and its zones,
/// then exits without installing hooks. Use it to check that imported layouts land on the right monitors.
/// </summary>
internal static class Diagnose
{
    public static int Run()
    {
        // A WinExe has no console of its own; write to the one it was started from.
        Windows.Interop.ConsoleAttachment.AttachToParent();

        (FancyZonesCloneConfig? config, string? source) = ConfigLoader.Load();
        Console.WriteLine($"Config: {source}");

        foreach (MonitorDescriptor monitor in Monitors.GetAll())
        {
            LayoutSpec layout = LayoutResolver.Resolve(config, monitor);
            string layoutName = layout.CustomLayoutId is { } id
                ? LayoutResolver.FindCustomLayout(config, id)?.Name ?? $"missing custom layout {id}"
                : string.Create(CultureInfo.InvariantCulture, $"{layout.Kind} ({layout.ZoneCount})");
            bool matched = LayoutResolver.FindAssignment(config, monitor.Identity) is not null;

            Console.WriteLine();
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Display {monitor.Number}: {monitor.Identity.DeviceId} [{monitor.Identity.InstanceId}]"));
            Console.WriteLine($"  Serial:       {(monitor.Identity.SerialNumber.Length > 0 ? monitor.Identity.SerialNumber : "(none)")}");
            Console.WriteLine($"  Monitor area: {monitor.MonitorArea}");
            Console.WriteLine($"  Work area:    {monitor.WorkArea}");
            Console.WriteLine($"  Layout:       {layoutName}{(matched ? "" : " (default; no assignment matched)")}");

            foreach (Zone zone in LayoutResolver.ZoneSetFor(config, monitor).Zones)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"    Zone {zone.Index + 1}: {monitor.ToScreen(zone.Rect)}"));
            }
        }

        return 0;
    }
}
