using FancyZonesClone.Core.Configuration;
using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Snapping;

/// <summary>The modifier keys that change drag behaviour.</summary>
/// <param name="Shift">Shift toggles the overlay, depending on <see cref="FancyZonesCloneSettings.ShiftDrag"/>.</param>
/// <param name="Ctrl">Ctrl spans the window across every zone between the first zone hit and the current one.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct DragModifiers(bool Shift, bool Ctrl);
