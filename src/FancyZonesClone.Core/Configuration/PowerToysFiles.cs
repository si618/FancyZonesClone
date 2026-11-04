namespace FancyZonesClone.Core.Configuration;

/// <summary>The JSON files FancyZones keeps in <c>%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones</c>. Any may be missing.</summary>
public sealed record PowerToysFiles(
    string? CustomLayouts = null,
    string? AppliedLayouts = null,
    string? DefaultLayouts = null,
    string? LayoutHotkeys = null,
    string? Settings = null)
{
    /// <summary>FancyZones' data folder for the current user.</summary>
    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "PowerToys", "FancyZones");

    /// <summary>Reads whichever of the files exist in <paramref name="folder"/>.</summary>
    public static PowerToysFiles ReadFrom(string folder)
    {
        string? Read(string name)
        {
            string path = Path.Combine(folder, name);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        return new PowerToysFiles(
            Read("custom-layouts.json"),
            Read("applied-layouts.json"),
            Read("default-layouts.json"),
            Read("layout-hotkeys.json"),
            Read("settings.json"));
    }
}
