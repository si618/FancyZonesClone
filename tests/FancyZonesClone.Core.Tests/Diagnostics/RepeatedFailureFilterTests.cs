using FancyZonesClone.Core.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace FancyZonesClone.Core.Tests.Diagnostics;

public class RepeatedFailureFilterTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 26, 9, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan _quietPeriod = TimeSpan.FromMinutes(1);

    [Test]
    public async Task ShouldLog_FirstFailure_ReturnsTrue()
    {
        // Arrange
        var filter = new RepeatedFailureFilter(new FakeTimeProvider(_start), _quietPeriod);

        // Act
        bool log = filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Assert
        await Assert.That(log).IsTrue();
    }

    [Test]
    public async Task ShouldLog_SameFailureWithinTheQuietPeriod_ReturnsFalse()
    {
        // Arrange
        var time = new FakeTimeProvider(_start);
        var filter = new RepeatedFailureFilter(time, _quietPeriod);
        filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Act
        time.Advance(TimeSpan.FromMilliseconds(10));
        bool log = filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Assert
        await Assert.That(log).IsFalse();
    }

    [Test]
    public async Task ShouldLog_SameFailureAfterTheQuietPeriod_ReturnsTrue()
    {
        // Arrange
        var time = new FakeTimeProvider(_start);
        var filter = new RepeatedFailureFilter(time, _quietPeriod);
        filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Act
        time.Advance(_quietPeriod);
        bool log = filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Assert
        await Assert.That(log).IsTrue();
    }

    [Test]
    public async Task ShouldLog_DifferentMessageWithinTheQuietPeriod_ReturnsTrue()
    {
        // Arrange
        var filter = new RepeatedFailureFilter(new FakeTimeProvider(_start), _quietPeriod);
        filter.ShouldLog(new InvalidOperationException("no monitors"));

        // Act
        bool log = filter.ShouldLog(new InvalidOperationException("no zones"));

        // Assert
        await Assert.That(log).IsTrue();
    }

    [Test]
    public async Task ShouldLog_DifferentTypeWithTheSameMessage_ReturnsTrue()
    {
        // Arrange
        var filter = new RepeatedFailureFilter(new FakeTimeProvider(_start), _quietPeriod);
        filter.ShouldLog(new InvalidOperationException("failed"));

        // Act
        bool log = filter.ShouldLog(new NotSupportedException("failed"));

        // Assert
        await Assert.That(log).IsTrue();
    }
}
