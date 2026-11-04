using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Overlay;

/// <summary>A straight stroke centreline from (<see cref="X1"/>, <see cref="Y1"/>) to (<see cref="X2"/>, <see cref="Y2"/>).</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct Segment(float X1, float Y1, float X2, float Y2);
