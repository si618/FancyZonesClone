namespace FancyZonesClone.Core.Diagnostics;

/// <summary>
/// Decides whether to log a failure, so one that repeats on every timer tick is logged once per
/// <paramref name="quietPeriod"/> instead of filling the log and pushing its first occurrence out.
/// </summary>
/// <param name="time">The clock that measures the quiet period.</param>
/// <param name="quietPeriod">How long a repeat of the last logged failure stays unlogged.</param>
public sealed class RepeatedFailureFilter(TimeProvider time, TimeSpan quietPeriod)
{
    private Type? _lastType;
    private string? _lastMessage;
    private long _lastLogged;

    /// <summary>
    /// Returns whether to log <paramref name="exception"/>: true unless it has the type and message of the last
    /// failure logged, and the quiet period since then hasn't passed.
    /// </summary>
    public bool ShouldLog(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Type type = exception.GetType();
        bool repeat = type == _lastType && string.Equals(exception.Message, _lastMessage, StringComparison.Ordinal);
        if (repeat && time.GetElapsedTime(_lastLogged) < quietPeriod)
        {
            return false;
        }

        _lastType = type;
        _lastMessage = exception.Message;
        _lastLogged = time.GetTimestamp();
        return true;
    }
}
