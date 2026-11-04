using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FancyZonesClone.Core.Layouts;
using ZoneRect = FancyZonesClone.Core.Geometry.PixelRect;

namespace FancyZonesClone.Views;

/// <summary>Draws a layout's zones scaled into the control, keeping the work area's aspect ratio.</summary>
public sealed class ZonePreview : Control
{
    /// <summary>Defines the <see cref="Zones"/> property.</summary>
    public static readonly StyledProperty<IReadOnlyList<Zone>?> ZonesProperty =
        AvaloniaProperty.Register<ZonePreview, IReadOnlyList<Zone>?>(nameof(Zones));

    /// <summary>Defines the <see cref="ReferenceWidth"/> property.</summary>
    public static readonly StyledProperty<int> ReferenceWidthProperty =
        AvaloniaProperty.Register<ZonePreview, int>(nameof(ReferenceWidth), 1);

    /// <summary>Defines the <see cref="ReferenceHeight"/> property.</summary>
    public static readonly StyledProperty<int> ReferenceHeightProperty =
        AvaloniaProperty.Register<ZonePreview, int>(nameof(ReferenceHeight), 1);

    private static readonly IBrush _screen = new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80));
    private static readonly IBrush _zone = new SolidColorBrush(Color.FromArgb(0x90, 0x00, 0x78, 0xD7));
    private static readonly IPen _border = new Pen(Brushes.White, 1);

    static ZonePreview() => AffectsRender<ZonePreview>(ZonesProperty, ReferenceWidthProperty, ReferenceHeightProperty);

    /// <summary>
    /// The zones, relative to a work area of <see cref="ReferenceWidth"/> by <see cref="ReferenceHeight"/>. Each is
    /// labelled with its index plus one, as the drag overlay numbers it.
    /// </summary>
    public IReadOnlyList<Zone>? Zones
    {
        get => GetValue(ZonesProperty);
        set => SetValue(ZonesProperty, value);
    }

    /// <summary>The work area's width in pixels.</summary>
    public int ReferenceWidth
    {
        get => GetValue(ReferenceWidthProperty);
        set => SetValue(ReferenceWidthProperty, value);
    }

    /// <summary>The work area's height in pixels.</summary>
    public int ReferenceHeight
    {
        get => GetValue(ReferenceHeightProperty);
        set => SetValue(ReferenceHeightProperty, value);
    }

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
