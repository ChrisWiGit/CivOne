namespace CivOne.Sound.Playback.Adlib;

#nullable enable

/// <summary>
/// The driver's own pseudo-random generator: <c>seed = rotateRight(0x9248 + seed, 3)</c> on
/// sixteen bits.
/// </summary>
/// <remarks>
/// It drives the pitch jitter of the noise instruments. Reproducing it exactly, and letting the
/// caller choose the starting value, keeps renders deterministic - the same tune always produces
/// the same file.
/// </remarks>
internal sealed class AdlibRandomDelegate(int seed = 0)
{
    private const int Increment = 0x9248;
    private const int Mask = 0xFFFF;

    private int _seed = seed & Mask;

    /// <summary>Gets the current seed.</summary>
    public int Seed => _seed;

    /// <summary>
    /// Advances the generator and returns the new value.
    /// </summary>
    /// <returns>A pseudo-random 16-bit value.</returns>
    public int Next()
    {
        int value = (Increment + _seed) & Mask;
        _seed = ((value >> 3) | (value << 13)) & Mask;
        return _seed;
    }
}
