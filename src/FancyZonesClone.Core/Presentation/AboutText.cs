namespace FancyZonesClone.Core.Presentation;

/// <summary>Builds the about window's text: the version, the build, the links, and the libraries used.</summary>
public static class AboutText
{
    /// <summary>Who wrote FancyZonesClone.</summary>
    public const string Author = "Simon McKenna";

    /// <summary>The licence FancyZonesClone is released under.</summary>
    public const string License = "MIT";

    private const string RepositoryUrl = "https://github.com/si618/FancyZonesClone";

    /// <summary>The links under the version, in display order.</summary>
    public static IReadOnlyList<AboutLink> Links { get; } =
    [
        new("Source code", new Uri(RepositoryUrl)),
        new("Releases", new Uri($"{RepositoryUrl}/releases")),
        new("Licence", new Uri($"{RepositoryUrl}/blob/main/LICENSE")),
        new("Third-party notices", new Uri($"{RepositoryUrl}/blob/main/THIRD-PARTY-NOTICES.md")),
    ];

    /// <summary>The software FancyZonesClone ports or links, in display order.</summary>
    public static IReadOnlyList<AboutLibrary> Libraries { get; } =
    [
        new("PowerToys FancyZones", "MIT. The zone arithmetic, zone selection, and keyboard snapping are ported from it.", new Uri("https://github.com/microsoft/PowerToys")),
        new("Avalonia", "MIT. Draws the tray menu and the layouts, help, and about windows.", new Uri("https://github.com/AvaloniaUI/Avalonia")),
        new("SkiaSharp", "MIT. Renders Avalonia's graphics through Skia.", new Uri("https://github.com/mono/SkiaSharp")),
        new("HarfBuzzSharp", "MIT. Shapes Avalonia's text through HarfBuzz.", new Uri("https://github.com/mono/SkiaSharp")),
        new("MicroCom.Runtime", "MIT. Avalonia's AOT-safe COM interop.", new Uri("https://github.com/kekekeks/MicroCom")),
        new("MinVer", "Apache-2.0. Takes the version from the git tag at build time, so it doesn't ship.", new Uri("https://github.com/adamralph/minver")),
    ];

    /// <summary>Formats an assembly's informational version for display.</summary>
    /// <param name="informationalVersion">
    /// The value of <c>AssemblyInformationalVersionAttribute</c>, such as <c>1.2.0+3f2a9c1d…</c>, or <see langword="null"/>.
    /// </param>
    /// <returns>The version, with the commit shortened to seven characters, such as <c>1.2.0 (3f2a9c1)</c>.</returns>
    public static string Version(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "unknown";
        }

        int plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return informationalVersion;
        }

        string version = informationalVersion[..plus];
        string metadata = informationalVersion[(plus + 1)..];
        return metadata.Length == 0 ? version : $"{version} ({metadata[..Math.Min(7, metadata.Length)]})";
    }

    /// <summary>Describes the runtime and how the executable was compiled.</summary>
    /// <param name="frameworkDescription">The value of <c>RuntimeInformation.FrameworkDescription</c>, such as <c>.NET 10.0.0</c>.</param>
    /// <param name="nativeAot">Whether the process runs as a Native AOT executable, with no JIT.</param>
    public static string Runtime(string frameworkDescription, bool nativeAot)
    {
        ArgumentNullException.ThrowIfNull(frameworkDescription);

        return nativeAot
            ? $"{frameworkDescription}, compiled ahead of time with Native AOT."
            : $"{frameworkDescription}, running on the JIT. Release builds are compiled ahead of time with Native AOT.";
    }
}
