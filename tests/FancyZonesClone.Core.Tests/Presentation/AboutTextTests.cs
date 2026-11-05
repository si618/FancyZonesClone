using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Core.Tests.Presentation;

public class AboutTextTests
{
    [Test]
    [Arguments("1.2.0+3f2a9c1d8e7b6a5f4e3d2c1b0a9f8e7d6c5b4a39", "1.2.0 (3f2a9c1)")]
    [Arguments("0.0.0-alpha.0.34+abc", "0.0.0-alpha.0.34 (abc)")]
    [Arguments("1.2.0", "1.2.0")]
    [Arguments("1.2.0+", "1.2.0")]
    [Arguments(null, "unknown")]
    [Arguments("", "unknown")]
    public async Task Version_InformationalVersion_ShortensCommit(string? informationalVersion, string expected)
    {
        // Act
        string version = AboutText.Version(informationalVersion);

        // Assert
        await Assert.That(version).IsEqualTo(expected);
    }

    [Test]
    public async Task Runtime_NativeAot_SaysCompiledAheadOfTime()
    {
        // Act
        string runtime = AboutText.Runtime(".NET 10.0.0", nativeAot: true);

        // Assert
        await Assert.That(runtime).IsEqualTo(".NET 10.0.0, compiled ahead of time with Native AOT.");
    }

    [Test]
    public async Task Runtime_Jit_SaysReleaseBuildsAreNativeAot()
    {
        // Act
        string runtime = AboutText.Runtime(".NET 10.0.0", nativeAot: false);

        // Assert
        await Assert.That(runtime).Contains("running on the JIT");
        await Assert.That(runtime).Contains("Release builds are compiled ahead of time with Native AOT");
    }

    [Test]
    public async Task Links_All_PointAtTheRepository()
    {
        // Assert
        foreach (AboutLink link in AboutText.Links)
        {
            await Assert.That(link.Uri.AbsoluteUri).StartsWith("https://github.com/si618/FancyZonesClone");
        }
    }

    [Test]
    public async Task Libraries_All_UseHttpsAndNameALicence()
    {
        // Assert
        foreach (AboutLibrary library in AboutText.Libraries)
        {
            await Assert.That(library.Uri.Scheme).IsEqualTo(Uri.UriSchemeHttps);
            await Assert.That(library.Description).Matches("^(MIT|Apache-2.0)\\. ");
        }
    }
}
