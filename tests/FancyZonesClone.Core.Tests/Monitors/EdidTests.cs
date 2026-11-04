using System.Text;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Tests.Monitors;

public class EdidTests
{
    [Test]
    public async Task ReadSerialNumber_SerialDescriptorAfterTimingAndName_ReturnsItsText()
    {
        // Arrange
        byte[] edid = BaseBlock();
        edid[54] = 0x3A; // A detailed timing: non-zero pixel clock.
        edid[55] = 0x02;
        WriteDescriptor(edid, 72, 0xFC, "DELL U2720Q");
        WriteDescriptor(edid, 90, 0xFF, "CN0F3K7R");

        // Act
        string serial = Edid.ReadSerialNumber(edid);

        // Assert
        await Assert.That(serial).IsEqualTo("CN0F3K7R");
    }

    [Test]
    public async Task ReadSerialNumber_ThirteenCharactersWithNoLineFeed_ReturnsAllOfThem()
    {
        // Arrange
        byte[] edid = BaseBlock();
        WriteDescriptor(edid, 108, 0xFF, "ABCDEFGHIJKLM");

        // Act
        string serial = Edid.ReadSerialNumber(edid);

        // Assert
        await Assert.That(serial).IsEqualTo("ABCDEFGHIJKLM");
    }

    [Test]
    public async Task ReadSerialNumber_NoSerialDescriptor_ReturnsEmpty()
    {
        // Arrange
        byte[] edid = BaseBlock();
        WriteDescriptor(edid, 72, 0xFC, "Laptop panel");

        // Act
        string serial = Edid.ReadSerialNumber(edid);

        // Assert
        await Assert.That(serial).IsEmpty();
    }

    [Test]
    public async Task ReadSerialNumber_WrongHeader_ReturnsEmpty()
    {
        // Arrange
        byte[] edid = BaseBlock();
        WriteDescriptor(edid, 54, 0xFF, "CN0F3K7R");
        edid[0] = 0x01;

        // Act
        string serial = Edid.ReadSerialNumber(edid);

        // Assert
        await Assert.That(serial).IsEmpty();
    }

    [Test]
    public async Task ReadSerialNumber_ShorterThanTheBaseBlock_ReturnsEmpty()
    {
        // Act
        string serial = Edid.ReadSerialNumber(BaseBlock().AsSpan(0, 100));

        // Assert
        await Assert.That(serial).IsEmpty();
    }

    private static byte[] BaseBlock()
    {
        byte[] edid = new byte[128];
        byte[] header = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
        header.CopyTo(edid, 0);
        return edid;
    }

    // A display descriptor: zero pixel clock, the tag at byte 3, and text from byte 5 ended by a line feed and
    // padded with spaces, as the EDID standard lays it out.
    private static void WriteDescriptor(byte[] edid, int offset, byte tag, string text)
    {
        edid[offset + 3] = tag;
        byte[] padded = [.. Enumerable.Repeat((byte)0x20, 13)];
        byte[] ascii = Encoding.ASCII.GetBytes(text);
        ascii.CopyTo(padded, 0);
        if (ascii.Length < 13)
        {
            padded[ascii.Length] = 0x0A;
        }

        padded.CopyTo(edid, offset + 5);
    }
}
