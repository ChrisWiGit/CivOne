using System.Collections.Generic;

namespace CivOne.Screens;

/// <summary>
/// Cycles through a tune's arrangements in order, one step per call, wrapping back to the first.
/// </summary>
/// <remarks>
/// Unlike <see cref="Sound.Playback.ArrangementPickerDelegate"/>, which reproduces the original
/// driver's random pick for actual gameplay, the sound test wants a predictable order so every
/// arrangement of a tune gets heard when stepping through it repeatedly.
/// </remarks>
internal sealed class SequentialArrangementPickerDelegate
{
    private readonly Dictionary<string, int> _nextArrangement = [];

    /// <summary>
    /// Returns the arrangement the given tune is currently due to play, without advancing the cursor.
    /// </summary>
    /// <remarks>
    /// Callers must call <see cref="Advance"/> themselves once that arrangement has actually started
    /// playing. Retrying a tune whose render is still pending must keep asking for the same
    /// arrangement - advancing here on every call, including failed ones, would abandon a queued
    /// render for a new one each time and the tune would never become ready.
    /// </remarks>
    /// <param name="name">Name that identifies the tune, so each tune cycles independently.</param>
    /// <param name="count">How many arrangements the tune offers.</param>
    /// <returns>The index to play, always <c>0</c> when there is nothing to choose from.</returns>
    public int Pick(string name, int count) => count <= 1 ? 0 : _nextArrangement.GetValueOrDefault(name) % count;

    /// <summary>
    /// Moves the cursor to the arrangement after the one last returned by <see cref="Pick"/>.
    /// </summary>
    /// <param name="name">Name that identifies the tune.</param>
    /// <param name="count">How many arrangements the tune offers.</param>
    public void Advance(string name, int count)
    {
        if (count <= 1) return;

        _nextArrangement[name] = (_nextArrangement.GetValueOrDefault(name) % count) + 1;
    }
}
