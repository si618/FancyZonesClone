using TUnit.Assertions.Enums;
using FancyZonesClone.Core.Configuration;
using FancyZonesClone.Core.Geometry;
using FancyZonesClone.Core.Layouts;
using FancyZonesClone.Core.Monitors;
using FancyZonesClone.Core.Presentation;

namespace FancyZonesClone.Core.Tests.Presentation;

public class MonitorLayoutViewModelTests
{
    private static readonly MonitorDescriptor _monitor = new(
        1, new MonitorIdentity("GSM7863", "4&a&0&UID1"), new PixelRect(0, 0, 3000, 1000), new PixelRect(0, 0, 3000, 1000), 1);

    [Test]
    public async Task Constructor_MonitorWithDefaultLayout_SelectsPriorityGridOfThree()
    {
        // Act
        var viewModel = new MonitorLayoutViewModel(new FancyZonesCloneConfig(), _monitor, () => { });

        // Assert
        await Assert.That(viewModel.SelectedChoice.Name).IsEqualTo("Priority grid (3)");
        await Assert.That(viewModel.Zones.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Constructor_LayoutWithNoMatchingChoice_AddsACurrentChoiceFirst()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        LayoutResolver.Assign(config, _monitor.Identity, new LayoutSpec { Kind = LayoutType.Columns, ZoneCount = 7 });

        // Act
        var viewModel = new MonitorLayoutViewModel(config, _monitor, () => { });

        // Assert
        await Assert.That(viewModel.Choices[0]).IsSameReferenceAs(viewModel.SelectedChoice);
        await Assert.That(viewModel.SelectedChoice.Name).IsEqualTo("Current: Columns (7)");
    }

    [Test]
    public async Task SelectedChoice_UserPicksColumns_AssignsLayoutRefreshesZonesAndNotifies()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        int changes = 0;
        var viewModel = new MonitorLayoutViewModel(config, _monitor, () => changes++);
        List<string?> raised = [];
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        viewModel.SelectedChoice = viewModel.Choices.First(c => string.Equals(c.Name, "Columns (2)", StringComparison.Ordinal));

        // Assert
        await Assert.That(changes).IsEqualTo(1);
        await Assert.That(LayoutResolver.Resolve(config, _monitor).Kind).IsEqualTo(LayoutType.Columns);
        await Assert.That(viewModel.Zones[1].Rect.Left).IsEqualTo(1500 + 8);
        await Assert.That(raised).Contains(nameof(MonitorLayoutViewModel.Zones));
    }

    [Test]
    public async Task Choices_CustomLayoutInConfig_IsListedByName()
    {
        // Arrange
        var config = new FancyZonesCloneConfig();
        config.CustomLayouts.Add(new CustomLayout
        {
            Id = Guid.NewGuid(),
            Name = "5K2K",
            Kind = CustomLayoutType.Canvas,
            Canvas = new CanvasLayoutInfo { ReferenceWidth = 100, ReferenceHeight = 100, Zones = [new(0, 0, 100, 100)] },
        });

        // Act
        var viewModel = new MonitorLayoutViewModel(config, _monitor, () => { });

        // Assert
        await Assert.That(viewModel.Choices.Select(c => c.Name)).Contains("5K2K");
    }

    [Test]
    public async Task Zones_GridWithGapsInItsIndexes_KeepsTheIndexesTheOverlayNumbersBy()
    {
        // Arrange
        var layoutId = Guid.NewGuid();
        var config = new FancyZonesCloneConfig();
        config.CustomLayouts.Add(new CustomLayout
        {
            Id = layoutId,
            Name = "Gaps",
            Kind = CustomLayoutType.Grid,
            Grid = new GridLayoutInfo { Rows = 1, Columns = 3, RowPercents = [10000], ColumnPercents = [3333, 3333, 3334], CellChildMap = [[0, 2, 3]] },
        });
        LayoutResolver.Assign(config, _monitor.Identity, LayoutSpec.ForCustom(layoutId));

        // Act
        var viewModel = new MonitorLayoutViewModel(config, _monitor, () => { });

        // Assert
        await Assert.That(viewModel.Zones.Select(z => z.Index)).IsEquivalentTo([0, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task CreateLayout_SameChoiceTwice_ReturnsIndependentLayouts()
    {
        // Arrange
        LayoutChoice choice = LayoutChoice.All(new FancyZonesCloneConfig()).First(c => string.Equals(c.Name, "Rows (2)", StringComparison.Ordinal));

        // Act
        LayoutSpec first = choice.CreateLayout();
        LayoutSpec second = choice.CreateLayout();
        first.Spacing = 99;

        // Assert
        await Assert.That(second.Spacing).IsEqualTo(16);
    }
}
