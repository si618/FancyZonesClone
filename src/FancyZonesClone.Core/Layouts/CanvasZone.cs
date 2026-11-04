using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Layouts;

/// <summary>One zone of a canvas layout, in the reference work area's coordinates.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CanvasZone(int X, int Y, int Width, int Height);
