namespace FancyZonesClone.Core.Presentation;

/// <summary>One library in the about window's list.</summary>
/// <param name="Name">The library's name.</param>
/// <param name="Description">Its licence and what FancyZonesClone uses it for.</param>
/// <param name="Uri">Its source repository.</param>
public sealed record AboutLibrary(string Name, string Description, Uri Uri);
