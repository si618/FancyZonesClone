namespace FancyZonesClone.Core.Configuration;

/// <summary>The imported configuration, and anything that couldn't be carried over.</summary>
/// <param name="Config">The imported configuration.</param>
/// <param name="Warnings">What couldn't be carried over. Record equality compares this list by reference, not by its items.</param>
public sealed record PowerToysImportResult(FancyZonesCloneConfig Config, IReadOnlyList<string> Warnings);
