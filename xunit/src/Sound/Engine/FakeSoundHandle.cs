using System;
using CivOne.Sound.Engine;

namespace CivOne.UnitTests.Sound.Engine
{
    /// <summary>
    /// A running sound that only records what was asked of it.
    /// </summary>
    /// <remarks>
    /// Lets the ducking of music be examined without a mixer and without an audio device: what
    /// matters there is the order of pause, end and resume, not the samples.
    /// </remarks>
    internal sealed class FakeSoundHandle : ISoundHandle
    {
        private Action? _ended;
        private bool _endedRaised;

        /// <inheritdoc />
        public bool IsPlaying { get; private set; } = true;

        /// <inheritdoc />
        public bool IsPaused { get; private set; }

        /// <inheritdoc />
        public float Volume { get; set; } = 1f;

        /// <summary>Gets how often the sound was paused.</summary>
        public int PauseCount { get; private set; }

        /// <summary>Gets how often the sound was resumed.</summary>
        public int ResumeCount { get; private set; }

        /// <inheritdoc />
        public event Action? Ended
        {
            add
            {
                if (_endedRaised)
                {
                    value?.Invoke();
                    return;
                }

                _ended += value;
            }

            remove => _ended -= value;
        }

        /// <inheritdoc />
        public void Pause(TimeSpan fade = default)
        {
            PauseCount++;
            IsPaused = true;
        }

        /// <inheritdoc />
        public void Resume(TimeSpan fade = default)
        {
            ResumeCount++;
            IsPaused = false;
        }

        /// <inheritdoc />
        public void Stop(TimeSpan fade = default)
        {
            IsPlaying = false;
            RaiseEnded();
        }

        /// <summary>
        /// Ends the sound the way the mixer would.
        /// </summary>
        public void RaiseEnded()
        {
            if (_endedRaised) return;

            Action? ended = _ended;
            _ended = null;
            _endedRaised = true;
            IsPlaying = false;
            ended?.Invoke();
        }
    }
}
