using CivOne.Screens;
using System.Drawing;
using static CivOne.Common;

namespace CivOne.Units;

/// <summary>
/// Common utilities specific to confrontation logic.
/// Abstracts Common static class dependencies for dependency injection.
/// </summary>
public interface IConfrontCommonServices
{
    /// <summary>
    /// Add a screen to the rendering stack (for animations, dialogs, etc).
    /// </summary>
    void AddScreen(IScreen screen);

    /// <summary>
    /// Offset for gameplay area (used for nuke positioning and rendering).
    /// </summary>
    Point GamePlayOffset { get; }
}
