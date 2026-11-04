using System.Globalization;
using System.Text.Json;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Configuration;

/// <summary>
/// Converts FancyZones' JSON files into an <see cref="FancyZonesCloneConfig"/>, so existing custom layouts and
/// per-monitor assignments carry over without PowerToys installed.
/// </summary>
public static class PowerToysImporter
{
    private static readonly JsonDocumentOptions _options = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    /// <summary>Imports from the given file contents. Malformed files and entries are skipped with a warning.</summary>
    public static PowerToysImportResult Import(PowerToysFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var config = new FancyZonesCloneConfig();
        List<string> warnings = [];

        Parse(files.CustomLayouts, "custom-layouts.json", warnings, root => ImportCustomLayouts(root, config, warnings));
        Parse(files.AppliedLayouts, "applied-layouts.json", warnings, root => ImportAppliedLayouts(root, config, warnings));
        Parse(files.DefaultLayouts, "default-layouts.json", warnings, root => ImportDefaultLayouts(root, config, warnings));
        Parse(files.LayoutHotkeys, "layout-hotkeys.json", warnings, root => ImportHotkeys(root, config, warnings));
        Parse(files.Settings, "settings.json", warnings, root => ImportSettings(root, config.Settings));

        return new PowerToysImportResult(config, warnings);
    }

    /// <summary>Converts a FancyZones layout type name, such as <c>priority-grid</c>, to a <see cref="LayoutType"/>.</summary>
    public static LayoutType? ParseLayoutType(string? type) => type switch
    {
        "blank" => LayoutType.Blank,
        "focus" => LayoutType.Focus,
        "columns" => LayoutType.Columns,
        "rows" => LayoutType.Rows,
        "grid" => LayoutType.Grid,
        "priority-grid" => LayoutType.PriorityGrid,
        "custom" => LayoutType.Custom,
        _ => null,
    };

    private static void Parse(string? json, string fileName, List<string> warnings, Action<JsonElement> import)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json, _options);
            import(document.RootElement);
        }
        catch (JsonException ex)
        {
            warnings.Add($"{fileName}: not valid JSON ({ex.Message}); skipped.");
        }
        catch (InvalidOperationException ex)
        {
            warnings.Add($"{fileName}: unexpected structure ({ex.Message}); skipped.");
        }
    }

    private static void ImportCustomLayouts(JsonElement root, FancyZonesCloneConfig config, List<string> warnings)
    {
        foreach (JsonElement entry in Array(root, "custom-layouts"))
        {
            string name = String(entry, "name") ?? "Unnamed";
            if (!TryGuid(String(entry, "uuid"), out Guid id) || !TryProperty(entry, "info", out JsonElement info))
            {
                warnings.Add($"Custom layout '{name}': missing uuid or info; skipped.");
                continue;
            }

            CustomLayout? layout = String(entry, "type") switch
            {
                "grid" => ParseGrid(id, name, info),
                "canvas" => ParseCanvas(id, name, info),
                var other => null,
            };

            if (layout is null)
            {
                warnings.Add($"Custom layout '{name}': unsupported type or malformed info; skipped.");
                continue;
            }

            if (ZoneCalculator.ExceedsMaxZoneCount(layout))
            {
                warnings.Add(string.Create(CultureInfo.InvariantCulture, $"Custom layout '{name}': more than {LayoutSpec.MaxZoneCount} zones; skipped."));
                continue;
            }

            config.CustomLayouts.Add(layout);
        }
    }

    private static CustomLayout? ParseGrid(Guid id, string name, JsonElement info)
    {
        int rows = Int(info, "rows");
        int columns = Int(info, "columns");
        if (Ints(info, "rows-percentage") is not { } rowPercents || Ints(info, "columns-percentage") is not { } columnPercents)
        {
            return null;
        }

        List<IReadOnlyList<int>> map = [];
        foreach (JsonElement row in Array(info, "cell-child-map"))
        {
            if (Ints(row) is not { } cells)
            {
                return null;
            }

            map.Add(cells);
        }

        bool wellFormed = rows > 0 && columns > 0 &&
            rowPercents.Length == rows && columnPercents.Length == columns && map.Count == rows;
        return wellFormed
            ? new CustomLayout
            {
                Id = id,
                Name = name,
                Kind = CustomLayoutType.Grid,
                Grid = new GridLayoutInfo { Rows = rows, Columns = columns, RowPercents = rowPercents, ColumnPercents = columnPercents, CellChildMap = map },
                ShowSpacing = Bool(info, "show-spacing"),
                Spacing = Int(info, "spacing"),
                SensitivityRadius = Int(info, "sensitivity-radius", 20),
            }
            : null;
    }

    private static CustomLayout? ParseCanvas(Guid id, string name, JsonElement info)
    {
        int referenceWidth = Int(info, "ref-width");
        int referenceHeight = Int(info, "ref-height");
        if (referenceWidth <= 0 || referenceHeight <= 0)
        {
            return null;
        }

        List<JsonElement> entries = [.. Array(info, "zones")];
        if (entries.Exists(z => z.ValueKind != JsonValueKind.Object))
        {
            return null;
        }

        CanvasZone[] zones = [.. entries.Select(z => new CanvasZone(Int(z, "X"), Int(z, "Y"), Int(z, "width"), Int(z, "height")))];
        return new CustomLayout
        {
            Id = id,
            Name = name,
            Kind = CustomLayoutType.Canvas,
            Canvas = new CanvasLayoutInfo { ReferenceWidth = referenceWidth, ReferenceHeight = referenceHeight, Zones = zones },
            SensitivityRadius = Int(info, "sensitivity-radius", 20),
        };
    }

    private static void ImportAppliedLayouts(JsonElement root, FancyZonesCloneConfig config, List<string> warnings)
    {
        foreach (JsonElement entry in Array(root, "applied-layouts"))
        {
            if (!TryProperty(entry, "device", out JsonElement device) || !TryProperty(entry, "applied-layout", out JsonElement applied))
            {
                warnings.Add("Applied layout without a device or layout; skipped.");
                continue;
            }

            // Virtual desktops aren't supported yet: keep the first entry per monitor, as the default desktop's.
            var identity = new MonitorIdentity(
                String(device, "monitor") ?? "",
                String(device, "monitor-instance") ?? "",
                String(device, "serial-number") ?? "");

            if (identity.DeviceId.Length == 0 || config.Monitors.Any(a => a.Monitor.MatchesDeviceAndInstance(identity)))
            {
                continue;
            }

            if (ParseLayoutSpec(applied, config, warnings, $"monitor {identity.DeviceId}") is { } layout)
            {
                config.Monitors.Add(new MonitorLayoutAssignment { Monitor = identity, Layout = layout });
            }
        }
    }

    private static void ImportDefaultLayouts(JsonElement root, FancyZonesCloneConfig config, List<string> warnings)
    {
        foreach (JsonElement entry in Array(root, "default-layouts"))
        {
            if (!TryProperty(entry, "layout", out JsonElement layoutElement) ||
                ParseLayoutSpec(layoutElement, config, warnings, "default layout") is not { } layout)
            {
                continue;
            }

            switch (String(entry, "monitor-configuration"))
            {
                case "horizontal":
                    config.DefaultHorizontalLayout = layout;
                    break;
                case "vertical":
                    config.DefaultVerticalLayout = layout;
                    break;
                default:
                    warnings.Add($"Default layout for unknown monitor configuration '{String(entry, "monitor-configuration")}'; skipped.");
                    break;
            }
        }
    }

    private static LayoutSpec? ParseLayoutSpec(JsonElement element, FancyZonesCloneConfig config, List<string> warnings, string context)
    {
        if (ParseLayoutType(String(element, "type")) is not { } kind)
        {
            warnings.Add($"{context}: unknown layout type '{String(element, "type")}'; skipped.");
            return null;
        }

        var spec = new LayoutSpec
        {
            Kind = kind,
            ShowSpacing = Bool(element, "show-spacing"),
            Spacing = Int(element, "spacing"),
            ZoneCount = Int(element, "zone-count"),
            SensitivityRadius = Int(element, "sensitivity-radius", 20),
        };

        if (Int(element, "zone-count") > LayoutSpec.MaxZoneCount)
        {
            warnings.Add(string.Create(CultureInfo.InvariantCulture, $"{context}: zone count {Int(element, "zone-count")} capped at {LayoutSpec.MaxZoneCount}."));
        }

        if (kind != LayoutType.Custom)
        {
            return spec;
        }

        if (!TryGuid(String(element, "uuid"), out Guid id) || LayoutResolver.FindCustomLayout(config, id) is null)
        {
            warnings.Add($"{context}: custom layout {String(element, "uuid")} not found; skipped.");
            return null;
        }

        spec.CustomLayoutId = id;
        return spec;
    }

    private static void ImportHotkeys(JsonElement root, FancyZonesCloneConfig config, List<string> warnings)
    {
        foreach (JsonElement entry in Array(root, "layout-hotkeys"))
        {
            int key = Int(entry, "key", -1);
            if (key is < 0 or > 9 || !TryGuid(String(entry, "layout-id"), out Guid id))
            {
                warnings.Add(string.Create(CultureInfo.InvariantCulture, $"Layout hotkey {key}: invalid key or layout id; skipped."));
                continue;
            }

            if (LayoutResolver.FindCustomLayout(config, id) is null)
            {
                warnings.Add(string.Create(CultureInfo.InvariantCulture, $"Layout hotkey {key}: layout {id} isn't a custom layout; skipped."));
                continue;
            }

            config.LayoutHotkeys.Add(new LayoutHotkey { Key = key, LayoutId = id });
        }
    }

    private static void ImportSettings(JsonElement root, FancyZonesCloneSettings settings)
    {
        if (!TryProperty(root, "properties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        bool? Flag(string name)
        {
            return TryProperty(properties, name, out JsonElement p) && TryProperty(p, "value", out JsonElement v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? v.GetBoolean()
                : null;
        }

        string? Text(string name)
        {
            return TryProperty(properties, name, out JsonElement p) && TryProperty(p, "value", out JsonElement v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
        }

        int? Number(string name)
        {
            return TryProperty(properties, name, out JsonElement p) && TryProperty(p, "value", out JsonElement v) &&
                v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n)
                ? n
                : null;
        }

        settings.ShiftDrag = Flag("fancyzones_shiftDrag") ?? settings.ShiftDrag;
        settings.OverrideSnapHotkeys = Flag("fancyzones_overrideSnapHotkeys") ?? settings.OverrideSnapHotkeys;
        settings.QuickLayoutSwitch = Flag("fancyzones_quickLayoutSwitch") ?? settings.QuickLayoutSwitch;
        settings.AllowChildWindowSnap = Flag("fancyzones_allowChildWindowSnap") ?? settings.AllowChildWindowSnap;
        settings.ShowZoneNumbers = Flag("fancyzones_showZoneNumber") ?? settings.ShowZoneNumbers;
        settings.HighlightOpacity = Number("fancyzones_highlight_opacity") ?? settings.HighlightOpacity;
        settings.ZoneColor = Text("fancyzones_zoneColor") ?? settings.ZoneColor;
        settings.ZoneBorderColor = Text("fancyzones_zoneBorderColor") ?? settings.ZoneBorderColor;
        settings.ZoneHighlightColor = Text("fancyzones_zoneHighlightColor") ?? settings.ZoneHighlightColor;
        settings.ZoneNumberColor = Text("fancyzones_zoneNumberColor") ?? settings.ZoneNumberColor;

        if (Number("fancyzones_overlappingZonesAlgorithm") is { } algorithm && Enum.IsDefined((OverlappingZonesAlgorithm)algorithm))
        {
            settings.OverlappingZonesAlgorithm = (OverlappingZonesAlgorithm)algorithm;
        }

        // FancyZones stores exclusions as one string, one app per line.
        foreach (string app in (Text("fancyzones_excluded_apps") ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            settings.ExcludedApps.Add(app);
        }
    }

    private static IEnumerable<JsonElement> Array(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            yield return item;
        }
    }

    // Every reader below tolerates the wrong JSON type, so a malformed entry is skipped on its own instead of
    // throwing and losing the rest of the file.
    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value);
    }

    private static string? String(JsonElement element, string name) =>
        TryProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int Int(JsonElement element, string name, int fallback = 0) =>
        TryProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
            ? number
            : fallback;

    private static bool Bool(JsonElement element, string name) =>
        TryProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.True;

    // The named array's items as integers, or null when it's missing or any item isn't one.
    private static int[]? Ints(JsonElement element, string name) =>
        TryProperty(element, name, out JsonElement value) ? Ints(value) : null;

    private static int[]? Ints(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<int> numbers = [];
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int number))
            {
                return null;
            }

            numbers.Add(number);
        }

        return [.. numbers];
    }

    private static bool TryGuid(string? text, out Guid id) => Guid.TryParse(text, out id);
}
