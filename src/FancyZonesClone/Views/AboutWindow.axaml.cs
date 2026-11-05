using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Views;

/// <summary>Shows the version, the build, the author, the licence, the repository links, and the libraries used.</summary>
public sealed partial class AboutWindow : Window
{
    /// <summary>Creates the window for the running executable.</summary>
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>The version MinVer stamped from the git tag, with the short commit.</summary>
    public string Version { get; } =
        AboutText.Version(typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>The author and licence line.</summary>
    public string Credit { get; } = $"By {AboutText.Author}. Released under the {AboutText.License} licence.";

    /// <summary>The .NET version, and whether this is the Native AOT build. A Native AOT process has no JIT, so it can't emit code.</summary>
    public string Runtime { get; } = AboutText.Runtime(RuntimeInformation.FrameworkDescription, nativeAot: !RuntimeFeature.IsDynamicCodeSupported);

    /// <summary>The repository links.</summary>
    public IReadOnlyList<AboutLink> Links { get; } = AboutText.Links;

    /// <summary>The ported and linked libraries.</summary>
    public IReadOnlyList<AboutLibrary> Libraries { get; } = AboutText.Libraries;
}
