// Portions ported from Microsoft PowerToys FancyZones (FancyZonesLib/FancyZonesWindowProcessing.cpp, WindowUtils.cpp).
// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
namespace FancyZonesClone.Core.Windows;

/// <summary>Why a window can or can't be snapped.</summary>
public enum Processability
{
    /// <summary>The window can be snapped.</summary>
    Processable,

    /// <summary>The window is minimised.</summary>
    Minimized,

    /// <summary>The window isn't visible.</summary>
    NotVisible,

    /// <summary>The window is a tool window, such as a floating palette.</summary>
    ToolWindow,

    /// <summary>The window is a child control, not a top-level window.</summary>
    NonRootWindow,

    /// <summary>The window is a popup with no resizable frame, such as a menu or the Start menu.</summary>
    NonProcessablePopupWindow,

    /// <summary>The window has a visible owner, such as a dialog, and child snapping is off.</summary>
    ChildWindow,

    /// <summary>The window's process is in the exclusion list.</summary>
    Excluded,
}
