namespace FancyZonesClone.Core.Presentation;

/// <summary>One headed list in the help window.</summary>
/// <param name="Title">The section heading.</param>
/// <param name="Items">The section's points, one per bullet.</param>
public sealed record HelpSection(string Title, IReadOnlyList<string> Items);
