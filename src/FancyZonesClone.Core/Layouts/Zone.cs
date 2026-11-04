// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/LayoutConfigurator.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using FancyZonesClone.Core.Geometry;
using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Layouts;

/// <summary>One zone: its index, shown as its number plus one, and its rectangle relative to the work area.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Zone(int Index, PixelRect Rect);
