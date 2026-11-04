using System.Text.Json;
using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;

namespace FancyZonesClone.Core.Tests.Configuration;

public class ConfigStoreTests
{
    [Test]
    public async Task Deserialize_SerializedConfig_RoundTripsLayoutsAndAssignments()
    {
        // Arrange
        var layoutId = Guid.NewGuid();
        var config = new FancyZonesCloneConfig();
        config.CustomLayouts.Add(new CustomLayout
        {
            Id = layoutId,
            Name = "Halves",
            Kind = CustomLayoutType.Grid,
            Grid = new GridLayoutInfo { Rows = 1, Columns = 2, RowPercents = [10000], ColumnPercents = [4000, 6000], CellChildMap = [[0, 1]] },
            ShowSpacing = true,
            Spacing = 8,
        });
        config.Monitors.Add(new MonitorLayoutAssignment { Monitor = new MonitorIdentity("GSM7863", "4&a&0&UID1", "S1"), Layout = LayoutSpec.ForCustom(layoutId) });
        config.Settings.ExcludedApps.Add("Teams.exe");

        // Act
        FancyZonesCloneConfig restored = ConfigStore.Deserialize(ConfigStore.Serialize(config));

        // Assert
        await Assert.That(restored.CustomLayouts[0].Grid!.ColumnPercents).IsEquivalentTo([4000, 6000], CollectionOrdering.Matching);
        await Assert.That(restored.CustomLayouts[0].Spacing).IsEqualTo(8);
        await Assert.That(restored.Monitors[0].Monitor).IsEqualTo(new MonitorIdentity("GSM7863", "4&a&0&UID1", "S1"));
        await Assert.That(restored.Monitors[0].Layout.CustomLayoutId).IsEqualTo(layoutId);
        await Assert.That(restored.Settings.ExcludedApps).IsEquivalentTo(["Teams.exe"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Serialize_LayoutType_WritesEnumNamesForHandEditing()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();

        // Act
        string json = ConfigStore.Serialize(config);

        // Assert
        await Assert.That(json).Contains("\"kind\": \"PriorityGrid\"");
    }

    [Test]
    public async Task Deserialize_HandEditedFileWithCommentsAndTrailingCommas_Loads()
    {
        // Arrange
        const string Json = /*lang=json*/ """
            {
              // hand-edited
              "settings": { "shiftDrag": false, },
            }
            """;

        // Act
        FancyZonesCloneConfig config = ConfigStore.Deserialize(Json);

        // Assert
        await Assert.That(config.Settings.ShiftDrag).IsFalse();
    }

    [Test]
    public async Task Load_AfterSave_ReadsBackTheSameConfig()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), "FancyZonesClone.Tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "config.json");
        var config = new FancyZonesCloneConfig { DefaultHorizontalLayout = new LayoutSpec { Kind = LayoutType.Columns, ZoneCount = 4 } };

        try
        {
            // Act
            ConfigStore.Save(config, path);
            FancyZonesCloneConfig? loaded = ConfigStore.Load(path);

            // Assert
            await Assert.That(loaded!.DefaultHorizontalLayout.ZoneCount).IsEqualTo(4);
            await Assert.That(File.Exists(path + ".tmp")).IsFalse();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task TryLoad_InvalidJson_ReturnsTheErrorInsteadOfThrowing()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), "FancyZonesClone.Tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "config.json");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(path, """{ "settings": { "shiftDrag": false "showZoneNumbers": true } }""").ConfigureAwait(false);

        try
        {
            // Act
            ConfigLoadResult result = ConfigStore.TryLoad(path);

            // Assert
            await Assert.That(result.Config).IsNull();
            await Assert.That(result.Error).IsNotNull();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task TryLoad_MissingFile_ReturnsNoConfigAndNoError()
    {
        // Act
        ConfigLoadResult result = ConfigStore.TryLoad(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "config.json"));

        // Assert
        await Assert.That(result.Config).IsNull();
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task SetAside_ExistingFile_MovesItBesideTheOriginalReplacingAnEarlierOne()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), "FancyZonesClone.Tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "config.json");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(path, "{ broken").ConfigureAwait(false);
        await File.WriteAllTextAsync(path + ".invalid", "older").ConfigureAwait(false);

        try
        {
            // Act
            string aside = ConfigStore.SetAside(path);

            // Assert
            await Assert.That(aside).IsEqualTo(path + ".invalid");
            await Assert.That(File.Exists(path)).IsFalse();
            await Assert.That(await File.ReadAllTextAsync(aside).ConfigureAwait(false)).IsEqualTo("{ broken");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Load_MissingFile_ReturnsNull()
    {
        // Act
        FancyZonesCloneConfig? config = ConfigStore.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "config.json"));

        // Assert
        await Assert.That(config).IsNull();
    }

    [Test]
    public async Task Deserialize_EmptyObject_KeepsEveryCollectionEmpty()
    {
        // Act
        FancyZonesCloneConfig config = ConfigStore.Deserialize("{}");

        // Assert
        await Assert.That(config.CustomLayouts).IsEmpty();
        await Assert.That(config.Monitors).IsEmpty();
        await Assert.That(config.LayoutHotkeys).IsEmpty();
        await Assert.That(config.Settings.ExcludedApps).IsEmpty();
        await Assert.That(config.DefaultHorizontalLayout.Kind).IsEqualTo(LayoutType.PriorityGrid);
    }

    [Test]
    public async Task Deserialize_SettingsWithoutExcludedApps_KeepsExcludedAppsEmpty()
    {
        // Act
        FancyZonesCloneConfig config = ConfigStore.Deserialize(/*lang=json*/ """{ "settings": { "shiftDrag": false } }""");

        // Assert
        await Assert.That(config.Settings.ExcludedApps).IsEmpty();
        await Assert.That(config.Settings.ShiftDrag).IsFalse();
    }

    [Test]
    [Arguments(/*lang=json*/ """{ "settings": null }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": null }""")]
    [Arguments(/*lang=json*/ """{ "monitors": null }""")]
    [Arguments(/*lang=json*/ """{ "layoutHotkeys": null }""")]
    [Arguments(/*lang=json*/ """{ "settings": { "excludedApps": null } }""")]
    [Arguments(/*lang=json*/ """{ "defaultHorizontalLayout": null }""")]
    [Arguments(/*lang=json*/ """{ "defaultVerticalLayout": null }""")]
    [Arguments(/*lang=json*/ """{ "settings": { "zoneColor": null } }""")]
    [Arguments(/*lang=json*/ """{ "settings": { "excludedApps": [ "Teams.exe", null ] } }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": [ null ] }""")]
    [Arguments(/*lang=json*/ """{ "monitors": [ null ] }""")]
    [Arguments(/*lang=json*/ """{ "layoutHotkeys": [ null ] }""")]
    [Arguments(/*lang=json*/ """{ "monitors": [ { "monitor": null, "layout": {} } ] }""")]
    [Arguments(/*lang=json*/ """{ "monitors": [ { "monitor": { "deviceId": "GSM7863", "instanceId": "1" }, "layout": null } ] }""")]
    [Arguments(/*lang=json*/ """{ "monitors": [ { "monitor": { "deviceId": null, "instanceId": "1" }, "layout": {} } ] }""")]
    [Arguments(/*lang=json*/ """{ "monitors": [ { "monitor": { "deviceId": "GSM7863" }, "layout": {} } ] }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": [ { "id": "6b7d1c1e-3f5a-4a63-9a0e-0f4c6c1d2e3f", "name": null, "kind": "Grid" } ] }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": [ { "id": "6b7d1c1e-3f5a-4a63-9a0e-0f4c6c1d2e3f", "name": "G", "kind": "Grid", "grid": { "rows": 2, "columns": 1, "rowPercents": [5000, 5000], "columnPercents": [10000], "cellChildMap": [[0], null] } } ] }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": [ { "id": "6b7d1c1e-3f5a-4a63-9a0e-0f4c6c1d2e3f", "name": "G", "kind": "Grid", "grid": { "rows": 1, "columns": 1, "rowPercents": null, "columnPercents": [10000], "cellChildMap": [[0]] } } ] }""")]
    [Arguments(/*lang=json*/ """{ "customLayouts": [ { "id": "6b7d1c1e-3f5a-4a63-9a0e-0f4c6c1d2e3f", "name": "C", "kind": "Canvas", "canvas": { "referenceWidth": 100, "referenceHeight": 100, "zones": null } } ] }""")]
    public async Task Deserialize_NullWhereAValueIsRequired_ThrowsJsonException(string json)
    {
        // Act
        Exception? thrown = Record(() => ConfigStore.Deserialize(json));

        // Assert
        await Assert.That(thrown).IsTypeOf<JsonException>();
    }

    [Test]
    public async Task TryLoad_NullSection_ReturnsTheErrorInsteadOfTheConfig()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), "FancyZonesClone.Tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "config.json");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(path, /*lang=json*/ """{ "monitors": [ null ] }""").ConfigureAwait(false);

        try
        {
            // Act
            ConfigLoadResult result = ConfigStore.TryLoad(path);

            // Assert
            await Assert.That(result.Config).IsNull();
            await Assert.That(result.Error).Contains("monitors[0]");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // Returns what the action throws, or null, so a test can act and assert separately.
    private static Exception? Record(Action action)
    {
        try
        {
            action();
            return null;
        }
#pragma warning disable CA1031 // The test asserts the type of whatever is thrown, so it must catch everything.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return ex;
        }
    }
}
