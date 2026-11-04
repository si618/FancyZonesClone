namespace FancyZonesClone.Core.Configuration;

/// <summary>The outcome of <see cref="ConfigStore.TryLoad"/>.</summary>
/// <param name="Config">The loaded config, or null when the file is missing or invalid.</param>
/// <param name="Error">Why the file couldn't be loaded, or null when it loaded or doesn't exist.</param>
public sealed record ConfigLoadResult(FancyZonesCloneConfig? Config, string? Error);
