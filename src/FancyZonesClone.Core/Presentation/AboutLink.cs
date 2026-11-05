namespace FancyZonesClone.Core.Presentation;

/// <summary>One link in the about window.</summary>
/// <param name="Label">The link's text.</param>
/// <param name="Uri">Where the link goes.</param>
public sealed record AboutLink(string Label, Uri Uri);
