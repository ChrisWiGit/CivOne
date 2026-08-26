using System;

namespace CivOne.Sound.Opl;

#nullable enable

/// <summary>
/// An FM synthesis chip of the OPL family, driven the way the original hardware was: by writing
/// registers and then asking it for samples.
/// </summary>
/// <remarks>
/// Keeping this behind an interface lets the player stay unaware of which chip it drives, so an
/// OPL3 implementation can be added later without touching the parser, the score or the player.
/// </remarks>
internal interface IOplChip
{
    /// <summary>Gets the rate at which <see cref="Render"/> produces samples, in Hz.</summary>
    int SampleRate { get; }

    /// <summary>Gets the number of channels the chip offers.</summary>
    int ChannelCount { get; }

    /// <summary>Returns the chip to its power-on state.</summary>
    void Reset();

    /// <summary>
    /// Writes one chip register.
    /// </summary>
    /// <param name="register">Register number, e.g. <c>0xB0</c> for the key-on of channel 0.</param>
    /// <param name="value">Value to write; only the low eight bits are used.</param>
    void WriteRegister(int register, int value);

    /// <summary>
    /// Renders the next samples and advances the chip by that much time.
    /// </summary>
    /// <param name="buffer">
    /// Buffer that receives one mono sample per element. One operator at full level reaches
    /// <c>±1</c>, so a busy passage sums well past that. Applying gain and keeping the result
    /// inside the output range is the mixer's job, not the chip's.
    /// </param>
    void Render(Span<float> buffer);
}
