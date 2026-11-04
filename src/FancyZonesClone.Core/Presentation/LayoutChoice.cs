using System.Globalization;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Presentation;

/// <summary>One entry in a monitor's layout picker: a template with a zone count, or a custom layout.</summary>
public sealed class LayoutChoice
{
    private readonly LayoutSpec _template;

    private LayoutChoice(string name, LayoutSpec template)
    {
        Name = name;
        _template = template;
    }

    /// <summary>The name shown in the picker.</summary>
    public string Name { get; }

    /// <summary>Lists the choices: the FancyZones templates at common zone counts, then every custom layout.</summary>
    public static IReadOnlyList<LayoutChoice> All(FancyZonesCloneConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<LayoutChoice> choices = [new("No zones", new LayoutSpec { Kind = LayoutType.Blank, ZoneCount = 0 })];
        choices.AddRange(Templates(LayoutType.PriorityGrid, "Priority grid", 2, 3, 4, 5));
        choices.AddRange(Templates(LayoutType.Columns, "Columns", 2, 3, 4));
        choices.AddRange(Templates(LayoutType.Rows, "Rows", 2, 3));
        choices.AddRange(Templates(LayoutType.Grid, "Grid", 4, 6, 9));
        choices.AddRange(Templates(LayoutType.Focus, "Focus", 3));
        choices.AddRange(config.CustomLayouts.Select(c => new LayoutChoice(c.Name, LayoutSpec.ForCustom(c.Id))));
        return choices;
    }

    /// <summary>A choice that keeps a layout with no matching entry, such as a template at an unusual zone count.</summary>
    public static LayoutChoice ForCurrent(LayoutSpec layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        string name = string.Create(CultureInfo.InvariantCulture, $"Current: {layout.Kind} ({layout.ZoneCount})");
        return new LayoutChoice(name, layout);
    }

    /// <summary>Whether <paramref name="layout"/> is this choice, ignoring spacing and sensitivity.</summary>
    public bool Matches(LayoutSpec layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        return layout.Kind == _template.Kind && layout.Kind switch
        {
            LayoutType.Custom => layout.CustomLayoutId == _template.CustomLayoutId,
            LayoutType.Blank => true,
            LayoutType.Focus or LayoutType.Columns or LayoutType.Rows or LayoutType.Grid or LayoutType.PriorityGrid =>
                layout.ZoneCount == _template.ZoneCount,
            _ => false,
        };
    }

    /// <summary>Creates a fresh layout for this choice, so editing one monitor's layout never changes another's.</summary>
    public LayoutSpec CreateLayout() => new()
    {
        Kind = _template.Kind,
        CustomLayoutId = _template.CustomLayoutId,
        ShowSpacing = _template.ShowSpacing,
        Spacing = _template.Spacing,
        ZoneCount = _template.ZoneCount,
        SensitivityRadius = _template.SensitivityRadius,
    };

    /// <inheritdoc/>
    public override string ToString() => Name;

    private static IEnumerable<LayoutChoice> Templates(LayoutType kind, string name, params int[] zoneCounts) =>
        zoneCounts.Select(count => new LayoutChoice(
            string.Create(CultureInfo.InvariantCulture, $"{name} ({count})"),
            new LayoutSpec { Kind = kind, ZoneCount = count }));
}
