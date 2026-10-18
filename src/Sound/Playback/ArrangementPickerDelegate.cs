using System;

namespace CivOne.Sound.Playback;



/// <summary>
/// Chooses which arrangement of a tune to play.
/// </summary>
/// <remarks>
/// The AdLib leader themes ship four versions of the same piece and the original picks between them
/// each time, so hearing the same leader twice does not sound identical. Keeping the choice in its
/// own class lets a test fix it.
/// </remarks>
internal sealed class ArrangementPickerDelegate(int? seed = null)
{
    private readonly Random _random = seed.HasValue ? new Random(seed.Value) : new Random();

    /// <summary>
    /// Picks an arrangement.
    /// </summary>
    /// <param name="count">How many arrangements the tune offers.</param>
    /// <returns>The index to play, always <c>0</c> when there is nothing to choose from.</returns>
    public int Pick(int count) => count <= 1 ? 0 : _random.Next(count);
}
