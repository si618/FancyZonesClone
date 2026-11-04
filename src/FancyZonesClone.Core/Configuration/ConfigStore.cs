using System.Text.Json;

namespace FancyZonesClone.Core.Configuration;

/// <summary>Loads and saves <see cref="FancyZonesCloneConfig"/> as JSON.</summary>
public static class ConfigStore
{
    /// <summary>The config file for the current user: <c>%APPDATA%\FancyZonesClone\config.json</c> on Windows.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FancyZonesClone", "config.json");

    /// <summary>Serialises <paramref name="config"/> to JSON.</summary>
    public static string Serialize(FancyZonesCloneConfig config) =>
        JsonSerializer.Serialize(config, ConfigJsonContext.Default.FancyZonesCloneConfig);

    /// <summary>Deserialises a config, throwing <see cref="JsonException"/> when the JSON is invalid.</summary>
    public static FancyZonesCloneConfig Deserialize(string json) =>
        JsonSerializer.Deserialize(json, ConfigJsonContext.Default.FancyZonesCloneConfig)
            ?? throw new JsonException("The config file is empty.");

    /// <summary>
    /// Loads the config at <paramref name="path"/>, or returns null when the file doesn't exist. Throws
    /// <see cref="JsonException"/> or <see cref="IOException"/> when it can't be read; <see cref="TryLoad"/> doesn't.
    /// </summary>
    public static FancyZonesCloneConfig? Load(string path) => File.Exists(path) ? Deserialize(File.ReadAllText(path)) : null;

    /// <summary>
    /// Loads the config at <paramref name="path"/> without throwing: a hand-edited file with a typo must not take
    /// the tray app down.
    /// </summary>
    /// <returns>The config, or null with no error when the file doesn't exist, or null with the reason it couldn't be loaded.</returns>
    public static ConfigLoadResult TryLoad(string path)
    {
        try
        {
            return new ConfigLoadResult(Load(path), Error: null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new ConfigLoadResult(Config: null, ex.Message);
        }
    }

    /// <summary>
    /// Moves the config at <paramref name="path"/> to <c>config.json.invalid</c> beside it, replacing an earlier
    /// one, so a fresh config can be saved without losing the user's edits.
    /// </summary>
    /// <returns>Where the file now is.</returns>
    public static string SetAside(string path)
    {
        string aside = path + ".invalid";
        File.Move(path, aside, overwrite: true);
        return aside;
    }

    /// <summary>
    /// Saves the config to <paramref name="path"/>. Writes a temporary file and moves it into place, so a crash
    /// mid-write never leaves a truncated config.
    /// </summary>
    public static void Save(FancyZonesCloneConfig config, string path)
    {
        ArgumentNullException.ThrowIfNull(config);

        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string temporary = path + ".tmp";
        File.WriteAllText(temporary, Serialize(config));
        File.Move(temporary, path, overwrite: true);
    }
}
