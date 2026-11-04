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
        uint[] pixels = new uint[300 * 300];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(0, 0, 300, 300))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 300, 300, zoneSet, [], _style with { ShowNumbers = true });

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
    [Arguments(40, 40, 12)]
    [Arguments(100, 100, 16)]
    [Arguments(2112, 1477, 246)]
    public async Task NumberHeight_ZoneSize_IsASixthOfTheSmallerSideAndAtLeastTheMinimum(int width, int height, int expected)
    {
        // Act
        int numberHeight = OverlayRenderer.NumberHeight(PixelRect.FromSize(0, 0, width, height));

        // Assert
        await Assert.That(numberHeight).IsEqualTo(expected);
    }

    [Test]
    public async Task Render_ZoneNumbersOn_AntiAliasesTheNumberEdges()
    {
        // Arrange
        uint[] pixels = new uint[300 * 300];
        var zoneSet = new ZoneSet([new Zone(0, new PixelRect(0, 0, 300, 300))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 300, 300, zoneSet, [], _style with { ShowNumbers = true });

        // Assert
        uint[] blended = [.. pixels.Where(pixel => pixel is not (0x80000080u or 0xFF000000u or 0xFFFFFFFFu))];
        await Assert.That(blended).IsNotEmpty();
        await Assert.That(blended.All(pixel => pixel >> 24 is >= 0x80 and < 0xFF)).IsTrue();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    [Arguments(10)]
    public async Task Render_EachZoneNumber_DrawsSolidStrokesInsideTheNumberBox(int number)
    {
        // Arrange
        uint[] pixels = new uint[300 * 300];
        var zoneSet = new ZoneSet([new Zone(number - 1, new PixelRect(0, 0, 300, 300))], sensitivityRadius: 0);

        // Act
        OverlayRenderer.Render(pixels, 300, 300, zoneSet, [], _style with { ShowNumbers = true });

        // Assert
        int[] inked = [.. Enumerable.Range(0, pixels.Length).Where(i => pixels[i] == 0xFF000000u)];
        await Assert.That(inked.Length).IsGreaterThan(100);
        await Assert.That(inked.All(i => i / 300 is >= 125 and < 175)).IsTrue();
    }
}
