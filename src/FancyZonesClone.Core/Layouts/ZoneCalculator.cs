// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/LayoutConfigurator.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;

namespace FancyZonesClone.Core.Layouts;

/// <summary>
/// Generates zone rectangles for a layout, relative to the work area's top-left corner. The arithmetic follows
/// FancyZones' <c>LayoutConfigurator</c> exactly, so a layout imported from PowerToys produces the same pixels.
/// </summary>
public static class ZoneCalculator
{
    /// <summary>The scale of grid row and column shares, which are in ten-thousandths.</summary>
    public const int PercentMultiplier = 10000;

    // FancyZones rejects a zone whose edge lies further than this outside the work area.
    private const int MaxNegativeSpacing = -20;

    // PriorityGrid differs from Grid only below this zone count, as in FancyZones.
    private const int PredefinedPriorityGridCount = 11;

    private static readonly GridLayoutInfo[] _priorityGrids =
    [
        Grid(1, 1, [10000], [10000], [[0]]),
        Grid(1, 2, [10000], [6667, 3333], [[0, 1]]),
        Grid(1, 3, [10000], [2500, 5000, 2500], [[0, 1, 2]]),
        Grid(2, 3, [5000, 5000], [2500, 5000, 2500], [[0, 1, 2], [0, 1, 3]]),
        Grid(2, 3, [5000, 5000], [2500, 5000, 2500], [[0, 1, 2], [3, 1, 4]]),
        Grid(3, 3, [3333, 3334, 3333], [2500, 5000, 2500], [[0, 1, 2], [0, 1, 3], [4, 1, 5]]),
        Grid(3, 3, [3333, 3334, 3333], [2500, 5000, 2500], [[0, 1, 2], [3, 1, 4], [5, 1, 6]]),
        Grid(3, 4, [3333, 3334, 3333], [2500, 2500, 2500, 2500], [[0, 1, 2, 3], [4, 1, 2, 5], [6, 1, 2, 7]]),
        Grid(3, 4, [3333, 3334, 3333], [2500, 2500, 2500, 2500], [[0, 1, 2, 3], [4, 1, 2, 5], [6, 1, 7, 8]]),
        Grid(3, 4, [3333, 3334, 3333], [2500, 2500, 2500, 2500], [[0, 1, 2, 3], [4, 1, 5, 6], [7, 1, 8, 9]]),
        Grid(3, 4, [3333, 3334, 3333], [2500, 2500, 2500, 2500], [[0, 1, 2, 3], [4, 1, 5, 6], [7, 8, 9, 10]]),
    ];

    /// <summary>
    /// Builds the zone set for <paramref name="layout"/> on a work area of the given size. Returns an empty zone set
    /// when the layout is blank, invalid, or references a missing custom layout.
    /// </summary>
    public static ZoneSet Calculate(LayoutSpec layout, CustomLayout? custom, int workAreaWidth, int workAreaHeight)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (workAreaWidth <= 0 || workAreaHeight <= 0)
        {
            return ZoneSet.Empty;
        }

        if (layout.Kind == LayoutType.Custom)
        {
            return custom is null
                ? ZoneSet.Empty
                : new ZoneSet(Custom(custom, workAreaWidth, workAreaHeight), custom.SensitivityRadius);
        }

        int spacing = layout.ShowSpacing ? layout.Spacing : 0;
        IReadOnlyList<Zone> zones = layout.Kind switch
        {
            LayoutType.Focus => Focus(workAreaWidth, workAreaHeight, layout.ZoneCount),
            LayoutType.Columns => Columns(workAreaWidth, workAreaHeight, layout.ZoneCount, spacing),
            LayoutType.Rows => Rows(workAreaWidth, workAreaHeight, layout.ZoneCount, spacing),
            LayoutType.Grid => EvenGrid(workAreaWidth, workAreaHeight, layout.ZoneCount, spacing),
            LayoutType.PriorityGrid => PriorityGrid(workAreaWidth, workAreaHeight, layout.ZoneCount, spacing),
            LayoutType.Blank => [],
            LayoutType.Custom => [],
            _ => [],
        };

        return new ZoneSet(zones, layout.SensitivityRadius);
    }

    /// <summary>Zones for a custom grid or canvas layout.</summary>
    public static IReadOnlyList<Zone> Custom(CustomLayout custom, int workAreaWidth, int workAreaHeight)
    {
        ArgumentNullException.ThrowIfNull(custom);

        return custom.Kind switch
        {
            CustomLayoutType.Grid when custom.Grid is not null =>
                CalculateGridZones(workAreaWidth, workAreaHeight, custom.Grid, custom.ShowSpacing ? custom.Spacing : 0),
            CustomLayoutType.Canvas when custom.Canvas is not null => Canvas(custom.Canvas, workAreaWidth, workAreaHeight),
            _ => [],
        };
    }

    /// <summary>Overlapping zones at 40% of the work area, each offset 50 pixels from the last.</summary>
    public static IReadOnlyList<Zone> Focus(int workAreaWidth, int workAreaHeight, int zoneCount)
    {
        var rect = new PixelRect(100, 100, 100 + (int)(workAreaWidth * 0.4), 100 + (int)(workAreaHeight * 0.4));
        int increment = zoneCount <= 1 ? 0 : 50;
        List<Zone> zones = [];

        for (int i = 0; i < zoneCount; i++)
        {
            if (!TryAdd(zones, new Zone(i, rect)))
            {
                return [];
            }

            rect = rect.Offset(increment, increment);
        }

        return zones;
    }

    /// <summary>Equal-height rows, sized so they sum exactly to the work area less the spacing.</summary>
    public static IReadOnlyList<Zone> Rows(int workAreaWidth, int workAreaHeight, int zoneCount, int spacing)
    {
        if (zoneCount <= 0)
        {
            return [];
        }

        long totalWidth = workAreaWidth - (spacing * 2L);
        long totalHeight = workAreaHeight - (spacing * (zoneCount + 1L));
        long top = spacing;
        List<Zone> zones = [];

        for (int i = 0; i < zoneCount; i++)
        {
            long right = totalWidth + spacing;
            long bottom = top + ((i + 1) * totalHeight / zoneCount) - (i * totalHeight / zoneCount);

            if (!TryAdd(zones, new Zone(i, new PixelRect(spacing, (int)top, (int)right, (int)bottom))))
            {
                return [];
            }

            top = bottom + spacing;
        }

        return zones;
    }

    /// <summary>Equal-width columns, sized so they sum exactly to the work area less the spacing.</summary>
    public static IReadOnlyList<Zone> Columns(int workAreaWidth, int workAreaHeight, int zoneCount, int spacing)
    {
        if (zoneCount <= 0)
        {
            return [];
        }

        long totalWidth = workAreaWidth - (spacing * (zoneCount + 1L));
        long totalHeight = workAreaHeight - (spacing * 2L);
        long left = spacing;
        List<Zone> zones = [];

        for (int i = 0; i < zoneCount; i++)
        {
            long right = left + ((i + 1) * totalWidth / zoneCount) - (i * totalWidth / zoneCount);
            long bottom = totalHeight + spacing;

            if (!TryAdd(zones, new Zone(i, new PixelRect((int)left, spacing, (int)right, (int)bottom))))
            {
                return [];
            }

            left = right + spacing;
        }

        return zones;
    }

    /// <summary>The most square grid that fits the zone count. The last zone absorbs any leftover cells.</summary>
    public static IReadOnlyList<Zone> EvenGrid(int workAreaWidth, int workAreaHeight, int zoneCount, int spacing)
    {
        if (zoneCount <= 0)
        {
            return [];
        }

        int rows = 1;
        while (zoneCount / rows >= rows)
        {
            rows++;
        }

        rows--;
        int columns = (zoneCount / rows) + (zoneCount % rows == 0 ? 0 : 1);

        int[] rowPercents = new int[rows];
        for (int row = 0; row < rows; row++)
        {
            rowPercents[row] = (PercentMultiplier * (row + 1) / rows) - (PercentMultiplier * row / rows);
        }

        int[] columnPercents = new int[columns];
        for (int col = 0; col < columns; col++)
        {
            columnPercents[col] = (PercentMultiplier * (col + 1) / columns) - (PercentMultiplier * col / columns);
        }

        var cellChildMap = new IReadOnlyList<int>[rows];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            int[] cells = new int[columns];
            for (int col = 0; col < columns; col++)
            {
                cells[col] = index++;
                if (index == zoneCount)
                {
                    index--;
                }
            }

            cellChildMap[row] = cells;
        }

        return CalculateGridZones(workAreaWidth, workAreaHeight, Grid(rows, columns, rowPercents, columnPercents, cellChildMap), spacing);
    }

    /// <summary>FancyZones' default family: hand-tuned grids up to ten zones, then an even grid.</summary>
    public static IReadOnlyList<Zone> PriorityGrid(int workAreaWidth, int workAreaHeight, int zoneCount, int spacing) =>
        zoneCount switch
        {
            <= 0 => [],
            < PredefinedPriorityGridCount => CalculateGridZones(workAreaWidth, workAreaHeight, _priorityGrids[zoneCount - 1], spacing),
            _ => EvenGrid(workAreaWidth, workAreaHeight, zoneCount, spacing),
        };

    /// <summary>
    /// Zones for a grid. Each zone spans the cells that share its index, starting from its top-left cell, with full
    /// spacing at the work area's edges and half spacing between zones.
    /// </summary>
    public static IReadOnlyList<Zone> CalculateGridZones(int workAreaWidth, int workAreaHeight, GridLayoutInfo grid, int spacing)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!IsWellFormed(grid))
        {
            return [];
        }

        (long[]? rowStart, long[]? rowEnd) = Extents(grid.RowPercents, workAreaHeight);
        (long[]? columnStart, long[]? columnEnd) = Extents(grid.ColumnPercents, workAreaWidth);
        IReadOnlyList<IReadOnlyList<int>> map = grid.CellChildMap;
        List<Zone> zones = [];

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int col = 0; col < grid.Columns; col++)
            {
                int index = map[row][col];
                bool isTopLeftCell = (row == 0 || map[row - 1][col] != index) && (col == 0 || map[row][col - 1] != index);
                if (!isTopLeftCell)
                {
                    continue;
                }

                int maxRow = row;
                while (maxRow + 1 < grid.Rows && map[maxRow + 1][col] == index)
                {
                    maxRow++;
                }

                int maxCol = col;
                while (maxCol + 1 < grid.Columns && map[row][maxCol + 1] == index)
                {
                    maxCol++;
                }

                long top = rowStart[row] + (row == 0 ? spacing : spacing / 2);
                long bottom = rowEnd[maxRow] - (maxRow == grid.Rows - 1 ? spacing : spacing / 2);
                long left = columnStart[col] + (col == 0 ? spacing : spacing / 2);
                long right = columnEnd[maxCol] - (maxCol == grid.Columns - 1 ? spacing : spacing / 2);

                if (!TryAdd(zones, new Zone(index, new PixelRect((int)left, (int)top, (int)right, (int)bottom))))
                {
                    return [];
                }
            }
        }

        return zones;
    }

    /// <summary>
    /// Scales canvas zones from the reference work area to the actual one. FancyZones converts the work area to
    /// logical pixels and each zone back to physical pixels with the monitor's DPI; the two conversions cancel, so
    /// only the scaling remains.
    /// </summary>
    public static IReadOnlyList<Zone> Canvas(CanvasLayoutInfo canvas, int workAreaWidth, int workAreaHeight)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (canvas.ReferenceWidth <= 0 || canvas.ReferenceHeight <= 0)
        {
            return [];
        }

        float width = workAreaWidth;
        float height = workAreaHeight;
        List<Zone> zones = [];

        foreach (CanvasZone zone in canvas.Zones)
        {
            float x = zone.X * width / canvas.ReferenceWidth;
            float y = zone.Y * height / canvas.ReferenceHeight;
            float zoneWidth = zone.Width * width / canvas.ReferenceWidth;
            float zoneHeight = zone.Height * height / canvas.ReferenceHeight;

            var rect = new PixelRect((int)x, (int)y, (int)(x + zoneWidth), (int)(y + zoneHeight));
            if (!TryAdd(zones, new Zone(zones.Count, rect)))
            {
                return [];
            }
        }

        return zones;
    }

    private static GridLayoutInfo Grid(int rows, int columns, int[] rowPercents, int[] columnPercents, IReadOnlyList<int>[] cellChildMap) =>
        new() { Rows = rows, Columns = columns, RowPercents = rowPercents, ColumnPercents = columnPercents, CellChildMap = cellChildMap };

    private static bool IsWellFormed(GridLayoutInfo grid) =>
        grid.Rows > 0 &&
        grid.Columns > 0 &&
        grid.RowPercents.Count == grid.Rows &&
        grid.ColumnPercents.Count == grid.Columns &&
        grid.CellChildMap.Count == grid.Rows &&
        grid.CellChildMap.All(row => row.Count == grid.Columns);

    // Start and end of each band. Computing both from the running total makes the bands sum exactly to the extent.
    private static (long[] Start, long[] End) Extents(IReadOnlyList<int> percents, int extent)
    {
        long[] start = new long[percents.Count];
        long[] end = new long[percents.Count];
        long total = 0;

        for (int i = 0; i < percents.Count; i++)
        {
            start[i] = total * extent / PercentMultiplier;
            total += percents[i];
            end[i] = total * extent / PercentMultiplier;
        }

        return (start, end);
    }

    // Mirrors FancyZones: one invalid or duplicate zone invalidates the whole layout.
    private static bool TryAdd(List<Zone> zones, Zone zone)
    {
        PixelRect rect = zone.Rect;
        bool valid = zone.Index >= 0 &&
            rect.Left >= MaxNegativeSpacing &&
            rect.Right >= MaxNegativeSpacing &&
            rect.Top >= MaxNegativeSpacing &&
            rect.Bottom >= MaxNegativeSpacing &&
            rect.Width >= 0 &&
            rect.Height >= 0;

        if (!valid || zones.Exists(z => z.Index == zone.Index))
        {
            return false;
        }

        zones.Add(zone);
        return true;
    }
}
