using System.Threading.Tasks;

namespace CivOne.Sound.Playback;

/// <summary>
/// Renders the tunes of a sound pack away from the game thread.
/// </summary>
/// <remarks>
/// Turning a tune into audio means emulating a sound chip, which takes far longer than a frame.
/// Everything behind this interface therefore runs on the thread pool; callers on the game thread
/// only ask and are told later.
/// </remarks>
internal interface ISoundPackRenderQueue
{
    /// <summary>
    /// Returns the wave file of a tune when it has already been rendered and is still up to date.
    /// </summary>
    /// <param name="packFolder">Folder of the sound pack.</param>
    /// <param name="fileName">File name of the tune inside that folder.</param>
    /// <param name="arrangement">Which arrangement is wanted.</param>
    /// <returns>Path of the wave file, or <c>null</c> when it still has to be rendered.</returns>
    string? TryGetCached(string packFolder, string fileName, int arrangement);

    /// <summary>
    /// Asks for a tune to be rendered.
    /// </summary>
    /// <remarks>
    /// Returns immediately. Asking twice for the same tune joins the render that is already running
    /// instead of starting a second one.
    /// </remarks>
    /// <param name="packFolder">Folder of the sound pack.</param>
    /// <param name="fileName">File name of the tune inside that folder.</param>
    /// <param name="arrangement">Which arrangement to render.</param>
    /// <returns>
    /// A task that yields the path of the wave file, or <c>null</c> when the tune could not be
    /// rendered.
    /// </returns>
    Task<string?> Request(string packFolder, string fileName, int arrangement);

    /// <summary>
    /// Starts rendering a whole pack in the background, so later requests find their file ready.
    /// </summary>
    /// <remarks>
    /// Returns immediately and does nothing when the pack is already being warmed up.
    /// </remarks>
    /// <param name="packFolder">Folder of the sound pack.</param>
    void WarmPack(string packFolder);
}
