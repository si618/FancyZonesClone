using FancyZonesClone.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace FancyZonesClone.Core.Tests.Diagnostics;

public class FileLoggerProviderTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 26, 9, 30, 15, 250, TimeSpan.Zero);
    private static readonly EventId _event = new(201);

    [Test]
    public async Task Log_Warning_WritesTheTimestampLevelCategoryEventAndMessage()
    {
        // Arrange
        string folder = NewFolder();
        string path = Path.Combine(folder, "app.log");
        var time = new FakeTimeProvider(_start);
        using var provider = new FileLoggerProvider(path, time);
        ILogger logger = provider.CreateLogger("ZoneEngine");

        try
        {
            // Act
            logger.Log(LogLevel.Warning, _event, "hotkey 3 ignored", exception: null, (state, _) => state);

            // Assert
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo($"2026-11-26T09:30:15.250Z warn ZoneEngine[201] hotkey 3 ignored{Environment.NewLine}");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Log_SecondEventAfterTimePasses_AppendsItWithTheLaterTimestamp()
    {
        // Arrange
        string folder = NewFolder();
        string path = Path.Combine(folder, "app.log");
        var time = new FakeTimeProvider(_start);
        using var provider = new FileLoggerProvider(path, time);
        ILogger logger = provider.CreateLogger("App");
        logger.Log(LogLevel.Information, _event, "first", exception: null, (state, _) => state);

        try
        {
            // Act
            time.Advance(TimeSpan.FromSeconds(2));
            logger.Log(LogLevel.Information, _event, "second", exception: null, (state, _) => state);

            // Assert
            string[] lines = await File.ReadAllLinesAsync(path);
            await Assert.That(lines.Length).IsEqualTo(2);
            await Assert.That(lines[0]).IsEqualTo("2026-11-26T09:30:15.250Z info App[201] first");
            await Assert.That(lines[1]).IsEqualTo("2026-11-26T09:30:17.250Z info App[201] second");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Log_WithException_WritesItOnTheLinesAfterTheMessage()
    {
        // Arrange
        string folder = NewFolder();
        string path = Path.Combine(folder, "app.log");
        using var provider = new FileLoggerProvider(path, new FakeTimeProvider(_start));
        ILogger logger = provider.CreateLogger("ZoneEngine");
        var exception = new InvalidOperationException("no monitors");

        try
        {
            // Act
            logger.Log(LogLevel.Error, _event, "callback failed", exception, (state, _) => state);

            // Assert
            string[] lines = await File.ReadAllLinesAsync(path);
            await Assert.That(lines[0]).IsEqualTo("2026-11-26T09:30:15.250Z fail ZoneEngine[201] callback failed");
            await Assert.That(lines[1]).IsEqualTo("System.InvalidOperationException: no monitors");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Log_FileAtTheSizeLimit_MovesItAsideAndStartsANewFile()
    {
        // Arrange
        string folder = NewFolder();
        string path = Path.Combine(folder, "app.log");
        using var provider = new FileLoggerProvider(path, new FakeTimeProvider(_start), maxBytes: 10);
        ILogger logger = provider.CreateLogger("App");
        logger.Log(LogLevel.Information, _event, "first", exception: null, (state, _) => state);

        try
        {
            // Act
            logger.Log(LogLevel.Information, _event, "second", exception: null, (state, _) => state);

            // Assert
            await Assert.That(await File.ReadAllTextAsync(path + ".old")).Contains("first");
            await Assert.That(await File.ReadAllTextAsync(path)).Contains("second");
            await Assert.That(await File.ReadAllTextAsync(path)).DoesNotContain("first");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Log_FullFileCannotBeMovedAside_StillAppendsTheEvent()
    {
        // Arrange
        string folder = NewFolder();
        string path = Path.Combine(folder, "app.log");
        using var provider = new FileLoggerProvider(path, new FakeTimeProvider(_start), maxBytes: 10);
        ILogger logger = provider.CreateLogger("App");
        logger.Log(LogLevel.Information, _event, "first", exception: null, (state, _) => state);

        // A folder in the old file's place makes the move fail on every platform.
        Directory.CreateDirectory(Path.Combine(path + ".old", "blocker"));

        try
        {
            // Act
            logger.Log(LogLevel.Information, _event, "second", exception: null, (state, _) => state);

            // Assert
            string[] lines = await File.ReadAllLinesAsync(path);
            await Assert.That(lines.Length).IsEqualTo(2);
            await Assert.That(lines[1]).EndsWith("second");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task Log_PathIsAFolder_LosesTheEventWithoutThrowing()
    {
        // Arrange
        string folder = NewFolder();
        Directory.CreateDirectory(folder);
        using var provider = new FileLoggerProvider(folder, new FakeTimeProvider(_start));
        ILogger logger = provider.CreateLogger("App");

        try
        {
            // Act
            logger.Log(LogLevel.Warning, _event, "lost", exception: null, (state, _) => state);

            // Assert
            await Assert.That(Directory.EnumerateFileSystemEntries(folder).Any()).IsFalse();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string NewFolder() => Path.Combine(Path.GetTempPath(), "FancyZonesClone.Tests", Guid.NewGuid().ToString("N"));
}
