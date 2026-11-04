using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Overlay;

namespace FancyZonesClone.Core.Tests.Overlay;

public class OverlayRendererTests
{
    private static readonly OverlayStyle _style = new(
        Zone: new RgbColor(0, 0, 255),
        Border: new RgbColor(255, 255, 255),
        Highlight: new RgbColor(255, 0, 0),
        Number: new RgbColor(0, 0, 0),
        Alpha: 128,
        ShowNumbers: false);

    [Test]
    public async Task Render_OneZone_FillsInsideDrawsBorderAndLeavesOutsideTransparent()
    {
        // Arrange
        uint[] pixels = new uint[20 * 20];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(5, 5, 15, 15))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 20, 20, zoneSet, [], _style);

        // Assert
        await Assert.That(pixels[(10 * 20) + 10]).IsEqualTo(0x80000080u);
        await Assert.That(pixels[(5 * 20) + 10]).IsEqualTo(0xFFFFFFFFu);
        await Assert.That(pixels[(2 * 20) + 2]).IsEqualTo(0u);
    }

    [Test]
    public async Task Render_HighlightedZone_UsesTheHighlightColour()
    {
        // Arrange
        uint[] pixels = new uint[20 * 20];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(0, 0, 20, 20))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 20, 20, zoneSet, [0], _style);

        // Assert
        await Assert.That(pixels[(10 * 20) + 10]).IsEqualTo(0x80800000u);
    }

    [Test]
    public async Task Render_ZoneNumbersOn_DrawsNumberPixelsAtTheCentre()
    {
        // Arrange
        uint[] pixels = new uint[100 * 100];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(0, 0, 100, 100))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 100, 100, zoneSet, [], _style with { ShowNumbers = true });

        // Assert
        await Assert.That(pixels).Contains(0xFF000000u);
    }

    [Test]
    public async Task Render_ZoneExtendingPastBuffer_ClipsWithoutThrowing()
    {
        // Arrange
        uint[] pixels = new uint[10 * 10];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(-20, -20, 50, 50))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 10, 10, zoneSet, [], _style);

        // Assert
        await Assert.That(pixels[55]).IsEqualTo(0x80000080u);
    }

    [Test]
    [Arguments("#0078D7", 0x00, 0x78, 0xD7)]
    [Arguments("F5FCFF", 0xF5, 0xFC, 0xFF)]
    [Arguments("not a colour", 1, 2, 3)]
    public async Task Parse_ColourText_ReadsHexOrFallsBack(string text, int r, int g, int b)
    {
        // Act
        var colour = RgbColor.Parse(text, new RgbColor(1, 2, 3));

        // Assert
        await Assert.That(colour).IsEqualTo(new RgbColor((byte)r, (byte)g, (byte)b));
    }

    [Test]
    public async Task ToPremultipliedBgra_HalfAlphaRed_ScalesChannelsByAlpha()
    {
        // Act
        uint pixel = new RgbColor(255, 0, 0).ToPremultipliedBgra(127);

        // Assert
        await Assert.That(pixel).IsEqualTo(0x7F7F0000u);
    }

    [Test]
    public async Task From_FiftyPercentOpacity_GivesAlpha127()
    {
        // Act
        var style = OverlayStyle.From(new FancyZonesCloneSettings { HighlightOpacity = 50 });

        // Assert
        await Assert.That(style.Alpha).IsEqualTo((byte)127);
    }

    [Test]
    [Arguments(100, 100, 2)]
    [Arguments(2100, 1477, 35)]
    public async Task NumberScale_ZoneSize_IsASixthOfTheSmallerSideInGlyphRows(int width, int height, int expected)
    {
        // Act
        int scale = OverlayRenderer.NumberScale(PixelRect.FromSize(0, 0, width, height));

        // Assert
        await Assert.That(scale).IsEqualTo(expected);
    }
}
