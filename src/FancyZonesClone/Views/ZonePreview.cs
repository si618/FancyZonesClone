using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FancyZonesClone.Core.Layouts;
using ZoneRect = FancyZonesClone.Core.Geometry.PixelRect;

namespace FancyZonesClone.Views;

/// <summary>Draws a layout's zones scaled into the control, keeping the work area's aspect ratio.</summary>
public sealed partial class ZonePreview : Control
{
    private static readonly IBrush _screen = new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80));
    private static readonly IBrush _zone = new SolidColorBrush(Color.FromArgb(0x90, 0x00, 0x78, 0xD7));
    private static readonly IPen _border = new Pen(Brushes.White, 1);

    static ZonePreview() => AffectsRender<ZonePreview>(ZonesProperty, ReferenceWidthProperty, ReferenceHeightProperty);

    /// <summary>
    /// The zones, relative to a work area of <see cref="ReferenceWidth"/> by <see cref="ReferenceHeight"/>. Each is
    /// labelled with its index plus one, as the drag overlay numbers it.
    /// </summary>
    [GeneratedStyledProperty]
    public partial IReadOnlyList<Zone>? Zones { get; set; }

    /// <summary>The work area's width in pixels.</summary>
    [GeneratedStyledProperty(DefaultValue = 1)]
    public partial int ReferenceWidth { get; set; }

    /// <summary>The work area's height in pixels.</summary>
    [GeneratedStyledProperty(DefaultValue = 1)]
    public partial int ReferenceHeight { get; set; }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        double scale = Math.Min(Bounds.Width / Math.Max(ReferenceWidth, 1), Bounds.Height / Math.Max(ReferenceHeight, 1));
        double offsetX = (Bounds.Width - (ReferenceWidth * scale)) / 2;
        context.FillRectangle(_screen, new Rect(offsetX, 0, ReferenceWidth * scale, ReferenceHeight * scale));

        foreach (Zone zone in Zones ?? [])
        {
            ZoneRect bounds = zone.Rect;
            var rect = new Rect(offsetX + (bounds.Left * scale), bounds.Top * scale, bounds.Width * scale, bounds.Height * scale);
            context.DrawRectangle(_zone, _border, rect);

            var label = new FormattedText(
                (zone.Index + 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, Math.Clamp(Math.Min(rect.Width, rect.Height) / 3, 8, 28), Brushes.White);
            context.DrawText(label, rect.Center - new Point(label.Width / 2, label.Height / 2));
        }
    }
}
