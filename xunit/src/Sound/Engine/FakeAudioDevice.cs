using System;
using CivOne.Sound.Engine;

namespace CivOne.UnitTests.Sound.Engine
{
    /// <summary>
    /// An audio device that never touches a sound card: the test asks it for samples itself.
    /// </summary>
    /// <remarks>
    /// This is what the <see cref="IAudioDevice"/> abstraction exists for. The whole mixer can be
    /// examined sample by sample without a running SDL window.
    /// </remarks>
    /// <param name="sampleRate">Rate to report, in Hz.</param>
    internal sealed class FakeAudioDevice(int sampleRate = 44100) : IAudioDevice
    {
        private AudioRenderCallback? _render;

        /// <inheritdoc />
        public int SampleRate { get; } = sampleRate;

        /// <summary>Gets whether <see cref="Stop"/> has been called.</summary>
        public bool Stopped { get; private set; }

        /// <summary>Gets whether <see cref="Dispose"/> has been called.</summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc />
        public void Start(AudioRenderCallback render) => _render = render;

        /// <inheritdoc />
        public void Stop()
        {
            Stopped = true;
            _render = null;
        }

        /// <inheritdoc />
        public void Dispose() => Disposed = true;

        /// <summary>
        /// Asks for the next samples, the way a real device would.
        /// </summary>
        /// <param name="count">How many samples to ask for.</param>
        /// <returns>The samples produced.</returns>
        public float[] Pull(int count)
        {
            var buffer = new float[count];
            _render?.Invoke(buffer);

            return buffer;
        }
    }
}
