using System.Globalization;
using Microsoft.Extensions.Logging;

namespace FancyZonesClone.Core.Diagnostics;

/// <summary>
/// Writes log events to one text file, so a user can attach it to a bug report. When the file reaches
/// <paramref name="maxBytes"/>, it's moved to <c>&lt;path&gt;.old</c>, replacing the previous one, so the two
/// files together stay under twice that size.
/// </summary>
/// <param name="path">The log file. Its folder is created on the first write.</param>
/// <param name="time">The clock for each event's timestamp.</param>
/// <param name="maxBytes">The size at which the file is moved aside.</param>
public sealed class FileLoggerProvider(string path, TimeProvider time, long maxBytes = FileLoggerProvider.DefaultMaxBytes) : ILoggerProvider
{
    /// <summary>The default size at which the log file is moved aside: 1 MiB.</summary>
    public const long DefaultMaxBytes = 1024 * 1024;

    private readonly Lock _gate = new();

    /// <summary>The log file for the current user: <c>%APPDATA%\FancyZonesClone\FancyZonesClone.log</c> on Windows.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FancyZonesClone", "FancyZonesClone.log");

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Each write opens and closes the file, so there's nothing to release.
    }

    private void Write(string category, LogLevel level, EventId eventId, string message, Exception? exception)
    {
        string timestamp = time.GetUtcNow().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        string entry = string.Create(CultureInfo.InvariantCulture, $"{timestamp} {LevelName(level)} {category}[{eventId.Id}] {message}{Environment.NewLine}");
        if (exception is not null)
        {
            entry += exception + Environment.NewLine;
        }

        lock (_gate)
        {
            try
            {
                if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                }

                var file = new FileInfo(path);
                if (file.Exists && file.Length >= maxBytes)
                {
                    File.Move(path, path + ".old", overwrite: true);
                }

                File.AppendAllText(path, entry);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The callers log from catch blocks that keep the hooks alive, so a log that can't be written must
                // not throw: the event is lost.
            }
        }
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => "none",
    };

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (IsEnabled(logLevel))
            {
                provider.Write(category, logLevel, eventId, formatter(state, exception), exception);
            }
        }
    }
}
