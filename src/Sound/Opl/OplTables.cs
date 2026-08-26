using System;

namespace CivOne.Sound.Opl;



/// <summary>
/// The lookup tables an OPL chip works from.
/// </summary>
/// <remarks>
/// <para>
/// The chip does all its arithmetic in the logarithmic domain: a quarter sine wave is stored as
/// attenuation, everything that makes a note quieter is added to it, and one exponential lookup at
/// the end turns the sum back into an amplitude. Reproducing that structure is what gives the
/// characteristic OPL sound, so the renderer follows it rather than multiplying floats.
/// </para>
/// <para>
/// One attenuation unit of the exponent is <c>1/256</c> of a halving, i.e. about 0.0235 dB.
/// The envelope, total level and key scaling all count in units of 0.1875 dB, which is why they are
/// shifted left by three before being added.
/// </para>
/// <para>
/// All tables are built on first use. They must never become eagerly initialized static fields:
/// a static initializer that throws would poison the type for the whole process.
/// </para>
/// </remarks>
internal static class OplTables
{
    /// <summary>Number of entries in a quarter of the sine wave.</summary>
    public const int QuarterSize = 256;

    /// <summary>Attenuation that is inaudible, used to clamp the exponent input.</summary>
    public const int MaxAttenuation = 0x1FFF;

    private static int[]? _logSine;
    private static int[]? _exponent;
    private static int[]? _keyScaleLevel;

    /// <summary>
    /// Gets the first quarter of a sine wave as attenuation, <c>-log2(sin(x)) * 256</c>.
    /// </summary>
    public static int[] LogSine => _logSine ??= BuildLogSine();

    /// <summary>
    /// Gets the mantissa table of the exponential, <c>(2^(i/256) - 1) * 1024</c>.
    /// </summary>
    public static int[] Exponent => _exponent ??= BuildExponent();

    /// <summary>
    /// Gets the key scale level base attenuation per F-number range, in units of 0.75 dB,
    /// indexed by the top four bits of the F-number.
    /// </summary>
    public static int[] KeyScaleLevel => _keyScaleLevel ??= BuildKeyScaleLevel();

    /// <summary>
    /// Turns an attenuation into a linear amplitude.
    /// </summary>
    /// <param name="attenuation">Attenuation in units of <c>1/256</c> of a halving.</param>
    /// <returns>Amplitude between 0 and 4084, where 4084 means no attenuation at all.</returns>
    public static int Amplitude(int attenuation)
    {
        if (attenuation < 0) attenuation = 0;
        if (attenuation > MaxAttenuation) attenuation = MaxAttenuation;

        int mantissa = Exponent[(QuarterSize - 1) - (attenuation & 0xFF)] + 1024;
        return (mantissa << 1) >> (attenuation >> 8);
    }

    private static int[] BuildLogSine()
    {
        var table = new int[QuarterSize];
        for (int i = 0; i < QuarterSize; i++)
        {
            double sine = Math.Sin((i + 0.5) * Math.PI / (2 * QuarterSize));
            table[i] = (int)Math.Round(-Math.Log2(sine) * QuarterSize);
        }

        return table;
    }

    private static int[] BuildExponent()
    {
        var table = new int[QuarterSize];
        for (int i = 0; i < QuarterSize; i++)
        {
            table[i] = (int)Math.Round((Math.Pow(2d, i / (double)QuarterSize) - 1d) * 1024d);
        }

        return table;
    }

    /// <summary>
    /// Builds the key scale level table: how much a note is attenuated for being high.
    /// </summary>
    /// <remarks>
    /// The slope is 6 dB per octave, so eight units of 0.75 dB per doubling of the F-number.
    /// The offset of 32 units cancels against the block term in
    /// <see cref="OplOperator.UpdateKeyScaleLevel"/>. Rounding up reproduces the chip's own table.
    /// </remarks>
    private static int[] BuildKeyScaleLevel()
    {
        var table = new int[16];
        for (int index = 1; index < table.Length; index++)
        {
            table[index] = (int)Math.Ceiling(32d + (8d * Math.Log2(index)));
        }

        return table;
    }
}
