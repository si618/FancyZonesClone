using FancyZonesClone.Core.Configuration;

namespace FancyZonesClone;

/// <summary>Loads the config, importing FancyZones' files on first run.</summary>
internal static class ConfigLoader
{
    /// <summary>
    /// Loads <see cref="ConfigStore.DefaultPath"/>. When it doesn't exist yet, imports PowerToys' FancyZones files
    /// if they're present, otherwise starts from defaults, and saves the result so later runs read it directly.
    /// A config that can't be read is moved aside first and treated as missing, so a typo never stops the app.
    /// </summary>
    /// <returns>The config, and a description of where it came from.</returns>
    public static (FancyZonesCloneConfig Config, string Source) Load()
    {
        string path = ConfigStore.DefaultPath;
        ConfigLoadResult loaded = ConfigStore.TryLoad(path);
        if (loaded.Config is { } existing)
        {
            return (existing, path);
        }

        string invalid = "";
        if (loaded.Error is { } error)
        {
            try
            {
                invalid = $"{path} was invalid ({error}) and was moved to {ConfigStore.SetAside(path)}; ";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Saving over the file would lose the user's edits, so run on defaults and leave it alone.
                return (new FancyZonesCloneConfig(), $"defaults; {path} is invalid ({error}) and couldn't be moved aside ({ex.Message})");
            }
        }

        (FancyZonesCloneConfig? config, string? source) = Directory.Exists(PowerToysFiles.DefaultFolder)
            ? (ImportPowerToys().Config, $"imported from {PowerToysFiles.DefaultFolder}, saved to {path}")
            : (new FancyZonesCloneConfig(), $"defaults, saved to {path}");

        try
        {
            ConfigStore.Save(config, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A folder that can't be written shouldn't stop the app: run on the in-memory config and save next time.
            source = $"{source} (save failed: {ex.Message})";
        }

        return (config, invalid + source);
    }

    /// <summary>Imports FancyZones' files from their usual folder.</summary>
    public static PowerToysImportResult ImportPowerToys() =>
        PowerToysImporter.Import(PowerToysFiles.ReadFrom(PowerToysFiles.DefaultFolder));
}
