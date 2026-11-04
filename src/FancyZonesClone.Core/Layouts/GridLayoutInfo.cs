namespace FancyZonesClone.Core.Layouts;

/// <summary>A grid layout: the share of each row and column, and the zone each cell belongs to.</summary>
public sealed class GridLayoutInfo
{
    /// <summary>The number of rows.</summary>
    public required int Rows { get; init; }

    /// <summary>The number of columns.</summary>
    public required int Columns { get; init; }

    /// <summary>Each row's height in ten-thousandths of the work area. The values sum to 10000.</summary>
    public required IReadOnlyList<int> RowPercents { get; init; }

    /// <summary>Each column's width in ten-thousandths of the work area. The values sum to 10000.</summary>
    public required IReadOnlyList<int> ColumnPercents { get; init; }

    /// <summary>The zone index of each cell, by row then column. Adjacent cells with the same index merge into one zone.</summary>
    public required IReadOnlyList<IReadOnlyList<int>> CellChildMap { get; init; }
}
