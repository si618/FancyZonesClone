// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/FancyZonesWindowProcessing.cpp, WindowUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
using System.Runtime.InteropServices;

namespace FancyZonesClone.Core.Windows;

/// <summary>The facts about a window that decide whether it can be snapped, read from Win32 by the caller.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct WindowTraits(
    bool IsMinimized,
    bool IsVisible,
    bool IsToolWindow,
    bool IsRoot,
    bool IsPopup,
    bool HasThickFrame,
    bool HasCaption,
    bool HasMinimizeOrMaximizeBox,
    bool HasVisibleOwner,
    string ProcessPath);
