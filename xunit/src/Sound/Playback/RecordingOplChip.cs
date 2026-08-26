using System;
using System.Collections.Generic;
using CivOne.Sound.Opl;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>One register write, as seen by the chip.</summary>
    /// <param name="Register">Register number.</param>
    /// <param name="Value">Value written.</param>
    public readonly record struct OplWrite(int Register, int Value);

    /// <summary>
    /// A chip that records what is written to it and produces silence.
    /// </summary>
    /// <remarks>
    /// It lets a test check exactly what the player asks the hardware to do, without depending on
    /// the synthesis itself.
    /// </remarks>
    public sealed class RecordingOplChip : IOplChip
    {
        private readonly List<OplWrite> _writes = [];
        private readonly byte[] _registers = new byte[0x100];

        /// <inheritdoc/>
        public int SampleRate => Opl2Chip.NativeSampleRate;

        /// <inheritdoc/>
        public int ChannelCount => Opl2Chip.Channels;

        /// <summary>Gets every register write since the last reset, in order.</summary>
        public IReadOnlyList<OplWrite> Writes => _writes;

        /// <summary>Gets how many samples were asked for.</summary>
        public int RenderedSamples { get; private set; }

        /// <summary>
        /// Gets the last value written to a register.
        /// </summary>
        /// <param name="register">Register number.</param>
        /// <returns>The stored value.</returns>
        public byte Register(int register) => _registers[register & 0xFF];

        /// <summary>Forgets everything recorded so far.</summary>
        public void Clear() => _writes.Clear();

        /// <inheritdoc/>
        public void Reset()
        {
            _writes.Clear();
            Array.Clear(_registers);
        }

        /// <inheritdoc/>
        public void WriteRegister(int register, int value)
        {
            _registers[register & 0xFF] = (byte)value;
            _writes.Add(new OplWrite(register & 0xFF, value & 0xFF));
        }

        /// <inheritdoc/>
        public void Render(Span<float> buffer)
        {
            RenderedSamples += buffer.Length;
            buffer.Clear();
        }
    }
}
