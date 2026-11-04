namespace FancyZonesClone.Windows.Interop;

/// <summary>Reads a null-terminated string out of a fixed-size Win32 character buffer.</summary>
internal static class CharBuffers
{
    public static string ToTerminatedString(ReadOnlySpan<char> buffer)
    {
        int end = buffer.IndexOf('\0');
        return (end < 0 ? buffer : buffer[..end]).ToString();
    }
}
