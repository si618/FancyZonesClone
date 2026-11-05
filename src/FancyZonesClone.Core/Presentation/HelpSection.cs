namespace FancyZonesClone.Core.Presentation;

/// <summary>One headed list in the help window.</summary>
/// <param name="Title">The section heading.</param>
/// <param name="Items">The section's points, one per bullet.</param>
/// <param name="FilePath">A file the section is about, which the window shows as a link above the bullets, or <see langword="null"/>.</param>
public sealed record HelpSection(string Title, IReadOnlyList<string> Items, string? FilePath = null);
