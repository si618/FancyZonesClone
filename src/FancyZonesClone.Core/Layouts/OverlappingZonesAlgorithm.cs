namespace FancyZonesClone.Core.Layouts;

/// <summary>Which zone wins when the cursor is over several overlapping zones.</summary>
public enum OverlappingZonesAlgorithm
{
    /// <summary>The zone with the smallest area.</summary>
    Smallest = 0,

    /// <summary>The zone with the largest area.</summary>
    Largest = 1,

    /// <summary>The zone whose equal slice of the overlap holds the cursor.</summary>
    Positional = 2,

    /// <summary>The zone whose centre is closest to the cursor.</summary>
    ClosestCenter = 3,
}
