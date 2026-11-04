namespace FancyZonesClone.Core.Layouts;

/// <summary>How a custom layout defines its zones.</summary>
public enum CustomLayoutType
{
    /// <summary>Rows and columns with merged cells, sized in ten-thousandths of the work area.</summary>
    Grid,

    /// <summary>Free-form, possibly overlapping rectangles, scaled from a reference work area.</summary>
    Canvas,
}
