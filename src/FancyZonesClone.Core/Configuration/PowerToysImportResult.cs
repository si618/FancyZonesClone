namespace FancyZonesClone.Core.Configuration;

/// <summary>The imported configuration, and anything that couldn't be carried over.</summary>
public sealed record PowerToysImportResult(FancyZonesCloneConfig Config, IReadOnlyList<string> Warnings);
