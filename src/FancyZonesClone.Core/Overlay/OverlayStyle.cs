using FancyZonesClone.Core.Configuration;

namespace FancyZonesClone.Core.Overlay;

/// <summary>The overlay's colours and options, resolved from <see cref="FancyZonesCloneSettings"/>.</summary>
public sealed record OverlayStyle(RgbColor Zone, RgbColor Border, RgbColor Highlight, RgbColor Number, byte Alpha, bool ShowNumbers)
{
    /// <summary>Resolves the style from settings, falling back to FancyZones' colours for anything unparseable.</summary>
    public static OverlayStyle From(FancyZonesCloneSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new OverlayStyle(
            RgbColor.Parse(settings.ZoneColor, new RgbColor(0xF5, 0xFC, 0xFF)),
            RgbColor.Parse(settings.ZoneBorderColor, new RgbColor(0xFF, 0xFF, 0xFF)),
            RgbColor.Parse(settings.ZoneHighlightColor, new RgbColor(0x00, 0x78, 0xD7)),
            RgbColor.Parse(settings.ZoneNumberColor, new RgbColor(0x00, 0x00, 0x00)),
            (byte)(Math.Clamp(settings.HighlightOpacity, 0, 100) * 255 / 100),
            settings.ShowZoneNumbers);
    }
}
