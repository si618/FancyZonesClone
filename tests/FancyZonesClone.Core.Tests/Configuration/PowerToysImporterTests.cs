using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Tests.Configuration;

public class PowerToysImporterTests
{
    private const string WideCanvasId = "{EEAD6306-0AB6-46F2-907B-B65D695C587F}";
    private const string SplitGridId = "{589B1027-5C18-4B14-B182-BD3FE2F91A0E}";

    // Shaped like a real FancyZones export: one canvas layout for an ultrawide, one grid for a portrait monitor.
    private const string CustomLayoutsJson = /*lang=json,strict*/ """
        {
          "custom-layouts": [
            {
              "uuid": "{EEAD6306-0AB6-46F2-907B-B65D695C587F}",
              "name": "5K2K",
              "type": "canvas",
              "info": {
                "ref-width": 5120,
                "ref-height": 2160,
                "zones": [
                  { "X": 0, "Y": 0, "width": 1477, "height": 2100 },
                  { "X": 1485, "Y": 0, "width": 2150, "height": 2100 },
                  { "X": 3643, "Y": 0, "width": 1477, "height": 1213 },
                  { "X": 3643, "Y": 1221, "width": 1477, "height": 879 }
                ],
                "sensitivity-radius": 20
              }
            },
            {
              "uuid": "{589B1027-5C18-4B14-B182-BD3FE2F91A0E}",
              "name": "Work Right",
              "type": "grid",
              "info": {
                "rows": 2,
                "columns": 1,
                "rows-percentage": [6872, 3128],
                "columns-percentage": [10000],
                "cell-child-map": [[0], [1]],
                "show-spacing": true,
                "spacing": 16,
                "sensitivity-radius": 15
              }
            }
          ]
        }
        """;

    private const string AppliedLayoutsJson = /*lang=json,strict*/ """
        {"applied-layouts":[
          {"device":{"monitor":"GSM7863","monitor-instance":"4&1a&0&UID100","serial-number":"SERIAL1","monitor-number":1,"virtual-desktop":"{00000000-0000-0000-0000-000000000000}"},
           "applied-layout":{"uuid":"{EEAD6306-0AB6-46F2-907B-B65D695C587F}","type":"custom","show-spacing":false,"spacing":0,"zone-count":4,"sensitivity-radius":20}},
          {"device":{"monitor":"GSM7863","monitor-instance":"4&1a&0&UID100","serial-number":"SERIAL1","monitor-number":1,"virtual-desktop":"{11111111-0000-0000-0000-000000000000}"},
           "applied-layout":{"uuid":"{00000000-0000-0000-0000-000000000000}","type":"rows","show-spacing":false,"spacing":0,"zone-count":2,"sensitivity-radius":20}},
          {"device":{"monitor":"DEL4233","monitor-instance":"4&1a&0&UID200","serial-number":"","monitor-number":2,"virtual-desktop":"{00000000-0000-0000-0000-000000000000}"},
           "applied-layout":{"uuid":"{589B1027-5C18-4B14-B182-BD3FE2F91A0E}","type":"custom","show-spacing":true,"spacing":16,"zone-count":2,"sensitivity-radius":20}},
          {"device":{"monitor":"BOE0A34","monitor-instance":"4&1a&0&UID300","serial-number":"0","monitor-number":1,"virtual-desktop":"{00000000-0000-0000-0000-000000000000}"},
           "applied-layout":{"uuid":"{00000000-0000-0000-0000-000000000000}","type":"priority-grid","show-spacing":true,"spacing":16,"zone-count":3,"sensitivity-radius":20}},
          {"device":{"monitor":"AOC3279","monitor-instance":"4&1a&0&UID400","serial-number":"","monitor-number":3,"virtual-desktop":"{00000000-0000-0000-0000-000000000000}"},
           "applied-layout":{"uuid":"{DEADBEEF-0000-0000-0000-000000000000}","type":"custom","show-spacing":false,"spacing":0,"zone-count":2,"sensitivity-radius":20}}
        ]}
        """;

    private const string DefaultLayoutsJson = /*lang=json,strict*/ """
        {"default-layouts":[
          {"monitor-configuration":"vertical","layout":{"uuid":"","type":"rows","show-spacing":true,"spacing":16,"zone-count":2,"sensitivity-radius":20}},
          {"monitor-configuration":"horizontal","layout":{"uuid":"{EEAD6306-0AB6-46F2-907B-B65D695C587F}","type":"custom","show-spacing":false,"spacing":0,"zone-count":0,"sensitivity-radius":0}}
        ]}
        """;

    private const string HotkeysJson = /*lang=json,strict*/ """{"layout-hotkeys":[{"key":0,"layout-id":"{EEAD6306-0AB6-46F2-907B-B65D695C587F}"}]}""";

    private const string SettingsJson = /*lang=json,strict*/ """
        {"properties":{
          "fancyzones_shiftDrag":{"value":true},
          "fancyzones_overrideSnapHotkeys":{"value":true},
          "fancyzones_overlappingZonesAlgorithm":{"value":3},
          "fancyzones_highlight_opacity":{"value":65},
          "fancyzones_zoneHighlightColor":{"value":"#112233"},
          "fancyzones_showZoneNumber":{"value":false},
          "fancyzones_excluded_apps":{"value":"Teams.exe\r\nGame.exe\r\n"}
        },"name":"FancyZones","version":"1.0"}
        """;

    private static PowerToysFiles AllFiles => new(CustomLayoutsJson, AppliedLayoutsJson, DefaultLayoutsJson, HotkeysJson, SettingsJson);

    [Test]
    public async Task Import_CanvasAndGridLayouts_KeepsIdsNamesAndGeometry()
    {
        // Act
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;

        // Assert
        await Assert.That(config.CustomLayouts.Select(c => c.Name)).IsEquivalentTo(["5K2K", "Work Right"], CollectionOrdering.Matching);
        CustomLayout canvas = config.CustomLayouts[0];
        await Assert.That(canvas.Id).IsEqualTo(Guid.Parse(WideCanvasId));
        await Assert.That(canvas.Canvas!.Zones[3]).IsEqualTo(new CanvasZone(3643, 1221, 1477, 879));
        CustomLayout grid = config.CustomLayouts[1];
        await Assert.That(grid.Grid!.RowPercents).IsEquivalentTo([6872, 3128], CollectionOrdering.Matching);
        await Assert.That(grid.Spacing).IsEqualTo(16);
        await Assert.That(grid.SensitivityRadius).IsEqualTo(15);
    }

    [Test]
    public async Task Import_AppliedLayouts_AssignsCustomAndTemplateLayoutsPerMonitor()
    {
        // Act
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;

        // Assert
        MonitorLayoutAssignment ultrawide = LayoutResolver.FindAssignment(config, new MonitorIdentity("GSM7863", "4&1a&0&UID100"))!;
        await Assert.That(ultrawide.Layout.CustomLayoutId).IsEqualTo(Guid.Parse(WideCanvasId));
        await Assert.That(ultrawide.Monitor.SerialNumber).IsEqualTo("SERIAL1");
        MonitorLayoutAssignment portrait = LayoutResolver.FindAssignment(config, new MonitorIdentity("DEL4233", "4&1a&0&UID200"))!;
        await Assert.That(portrait.Layout.CustomLayoutId).IsEqualTo(Guid.Parse(SplitGridId));
        MonitorLayoutAssignment laptop = LayoutResolver.FindAssignment(config, new MonitorIdentity("BOE0A34", "4&1a&0&UID300"))!;
        await Assert.That(laptop.Layout.Kind).IsEqualTo(LayoutType.PriorityGrid);
    }

    [Test]
    public async Task Import_SecondVirtualDesktopEntryForSameMonitor_KeepsTheFirst()
    {
        // Act
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;

        // Assert
        await Assert.That(config.Monitors.Count(m => string.Equals(m.Monitor.DeviceId, "GSM7863", StringComparison.Ordinal))).IsEqualTo(1);
    }

    [Test]
    public async Task Import_AppliedLayoutReferencingMissingCustomLayout_SkipsItWithAWarning()
    {
        // Act
        PowerToysImportResult result = PowerToysImporter.Import(AllFiles);

        // Assert
        await Assert.That(result.Config.Monitors.Any(m => string.Equals(m.Monitor.DeviceId, "AOC3279", StringComparison.Ordinal))).IsFalse();
        await Assert.That(result.Warnings).Contains(w => w.Contains("AOC3279", StringComparison.Ordinal));
    }

    [Test]
    public async Task Import_DefaultLayouts_SetsDefaultsPerOrientation()
    {
        // Act
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;

        // Assert
        await Assert.That(config.DefaultHorizontalLayout.CustomLayoutId).IsEqualTo(Guid.Parse(WideCanvasId));
        await Assert.That(config.DefaultVerticalLayout.Kind).IsEqualTo(LayoutType.Rows);
    }

    [Test]
    public async Task Import_LayoutHotkeys_MapsDigitToCustomLayout()
    {
        // Act
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;

        // Assert
        await Assert.That(config.LayoutHotkeys.Count).IsEqualTo(1);
        await Assert.That(config.LayoutHotkeys[0].Key).IsEqualTo(0);
        await Assert.That(config.LayoutHotkeys[0].LayoutId).IsEqualTo(Guid.Parse(WideCanvasId));
    }

    [Test]
    public async Task Import_Settings_CarriesBehaviourAndAppearanceOver()
    {
        // Act
        FancyZonesCloneSettings settings = PowerToysImporter.Import(AllFiles).Config.Settings;

        // Assert
        await Assert.That(settings.OverrideSnapHotkeys).IsTrue();
        await Assert.That(settings.OverlappingZonesAlgorithm).IsEqualTo(OverlappingZonesAlgorithm.ClosestCenter);
        await Assert.That(settings.HighlightOpacity).IsEqualTo(65);
        await Assert.That(settings.ZoneHighlightColor).IsEqualTo("#112233");
        await Assert.That(settings.ShowZoneNumbers).IsFalse();
        await Assert.That(settings.ExcludedApps).IsEquivalentTo(["Teams.exe", "Game.exe"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Import_MalformedFile_WarnsAndImportsTheRest()
    {
        // Arrange
        PowerToysFiles files = AllFiles with { Settings = "{ not json" };

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(files);

        // Assert
        await Assert.That(result.Warnings).Contains(w => w.StartsWith("settings.json", StringComparison.Ordinal));
        await Assert.That(result.Config.CustomLayouts.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("[6872.5, 3127.5]", "[[0], [1]]")]
    [Arguments("[\"6872\", \"3128\"]", "[[0], [1]]")]
    [Arguments("[6872, 3128]", "[0, 1]")]
    [Arguments("[6872, 3128]", "[[0], [\"1\"]]")]
    public async Task Import_MalformedGridEntry_SkipsOnlyThatLayoutWithAWarning(string rowPercents, string cellChildMap)
    {
        // Arrange
        string json = $$$"""
            {"custom-layouts":[
              {"uuid":"{11111111-1111-1111-1111-111111111111}","name":"Broken","type":"grid",
               "info":{"rows":2,"columns":1,"rows-percentage":{{{rowPercents}}},"columns-percentage":[10000],"cell-child-map":{{{cellChildMap}}}}},
              {"uuid":"{{{SplitGridId}}}","name":"Good","type":"grid",
               "info":{"rows":1,"columns":2,"rows-percentage":[10000],"columns-percentage":[5000,5000],"cell-child-map":[[0,1]]}}
            ]}
            """;

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles(CustomLayouts: json));

        // Assert
        await Assert.That(result.Config.CustomLayouts.Select(c => c.Name)).IsEquivalentTo(["Good"], CollectionOrdering.Matching);
        await Assert.That(result.Warnings).Contains(w => w.Contains("'Broken'", StringComparison.Ordinal));
    }

    [Test]
    public async Task Import_CanvasZoneNotAnObject_SkipsOnlyThatLayout()
    {
        // Arrange
        const string Json = /*lang=json,strict*/ """
            {"custom-layouts":[
              {"uuid":"{11111111-1111-1111-1111-111111111111}","name":"Broken","type":"canvas",
               "info":{"ref-width":100,"ref-height":100,"zones":[{"X":0,"Y":0,"width":50,"height":100}, 7]}},
              {"uuid":"{EEAD6306-0AB6-46F2-907B-B65D695C587F}","name":"Good","type":"canvas",
               "info":{"ref-width":100,"ref-height":100,"zones":[{"X":0,"Y":0,"width":50,"height":100}]}},
              "not a layout"
            ]}
            """;

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles(CustomLayouts: Json));

        // Assert
        await Assert.That(result.Config.CustomLayouts.Select(c => c.Name)).IsEquivalentTo(["Good"], CollectionOrdering.Matching);
        await Assert.That(result.Warnings.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Import_SettingOfTheWrongType_IgnoresThatSettingAndKeepsTheRest()
    {
        // Arrange
        const string Json = /*lang=json,strict*/ """
            {"properties":{
              "fancyzones_highlight_opacity":{"value":"65"},
              "fancyzones_shiftDrag":7,
              "fancyzones_showZoneNumber":{"value":false}
            }}
            """;

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles(Settings: Json));

        // Assert
        await Assert.That(result.Warnings).IsEmpty();
        await Assert.That(result.Config.Settings.HighlightOpacity).IsEqualTo(new FancyZonesCloneSettings().HighlightOpacity);
        await Assert.That(result.Config.Settings.ShowZoneNumbers).IsFalse();
    }

    [Test]
    public async Task Import_NoFiles_ReturnsDefaultsWithoutWarnings()
    {
        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles());

        // Assert
        await Assert.That(result.Warnings).IsEmpty();
        await Assert.That(result.Config.DefaultHorizontalLayout.Kind).IsEqualTo(LayoutType.PriorityGrid);
    }

    [Test]
    public async Task Import_UltrawideEndToEnd_ProducesFourZonesOnItsWorkArea()
    {
        // Arrange
        FancyZonesCloneConfig config = PowerToysImporter.Import(AllFiles).Config;
        var monitor = new MonitorDescriptor(
            1, new MonitorIdentity("GSM7863", "4&1a&0&UID100"), new PixelRect(0, 0, 5120, 2160), new PixelRect(0, 0, 5120, 2112), 1);

        // Act
        ZoneSet zoneSet = LayoutResolver.ZoneSetFor(config, monitor);

        // Assert
        await Assert.That(zoneSet.Zones.Count).IsEqualTo(4);
        await Assert.That(zoneSet.Zones[1].Rect).IsEqualTo(new PixelRect(1485, 0, 3635, 2053));
    }

    [Test]
    public async Task Import_ZoneCountAboveMax_CapsItWithAWarning()
    {
        // Arrange
        const string Json = /*lang=json,strict*/ """
            {"default-layouts":[
              {"monitor-configuration":"horizontal","layout":{"type":"focus","show-spacing":false,"spacing":0,"zone-count":100000,"sensitivity-radius":20}}
            ]}
            """;

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles(DefaultLayouts: Json));

        // Assert
        await Assert.That(result.Config.DefaultHorizontalLayout.ZoneCount).IsEqualTo(LayoutSpec.MaxZoneCount);
        await Assert.That(result.Warnings).Contains(w => w.Contains("capped at 128", StringComparison.Ordinal));
    }

    [Test]
    public async Task Import_CanvasWithMoreThanMaxZones_SkipsItWithAWarning()
    {
        // Arrange
        string zones = string.Join(',', Enumerable.Repeat(/*lang=json,strict*/ """{"X":0,"Y":0,"width":10,"height":10}""", LayoutSpec.MaxZoneCount + 1));
        string json = $$$"""
            {"custom-layouts":[
              {"uuid":"{11111111-1111-1111-1111-111111111111}","name":"Huge","type":"canvas",
               "info":{"ref-width":100,"ref-height":100,"zones":[{{{zones}}}]}}
            ]}
            """;

        // Act
        PowerToysImportResult result = PowerToysImporter.Import(new PowerToysFiles(CustomLayouts: json));

        // Assert
        await Assert.That(result.Config.CustomLayouts).IsEmpty();
        await Assert.That(result.Warnings).Contains(w => w.Contains("'Huge': more than 128 zones", StringComparison.Ordinal));
    }

    [Test]
    [Arguments("priority-grid", LayoutType.PriorityGrid)]
    [Arguments("blank", LayoutType.Blank)]
    [Arguments("focus", LayoutType.Focus)]
    public async Task ParseLayoutType_FancyZonesTypeName_MapsToLayoutType(string type, LayoutType expected)
    {
        // Act
        LayoutType? kind = PowerToysImporter.ParseLayoutType(type);

        // Assert
        await Assert.That(kind).IsEqualTo(expected);
    }
}
