using System;
using System.IO;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers reading the playing time back out of a wave file <see cref="WaveFileWriter"/> wrote -
    /// the accurate source <see cref="SoundPackPlaybackService.TryGetDuration"/> prefers over
    /// <see cref="CivOne.Sound.Cvl.SoundPackIndexEntry.TotalTicks"/>, which does not follow a tune's
    /// internal loop repeats.
    /// </summary>
    public sealed class WaveFileDurationDelegateTests : IDisposable
    {
        private readonly string _path = Path.GetTempFileName();
        private readonly WaveFileWriter _writer = new();
        private readonly WaveFileDurationDelegate _delegateUnderTest = new();

        public void Dispose() => File.Delete(_path);

        /// <summary>
        /// The duration is derived from the sample count and rate, not guessed from the file size.
        /// </summary>
        [Fact]
        public void TheDurationMatchesTheSampleCountAndRate()
        {
            _writer.Write(_path, new short[250], sampleRate: 100);

            Assert.True(_delegateUnderTest.TryRead(_path, out TimeSpan duration));
            Assert.Equal(TimeSpan.FromSeconds(2.5), duration);
        }

        /// <summary>
        /// A file too short to even hold a header must not be misread as a zero-length tune.
        /// </summary>
        [Fact]
        public void ATruncatedFileIsNotReadAsASound()
        {
            File.WriteAllBytes(_path, new byte[10]);

            Assert.False(_delegateUnderTest.TryRead(_path, out TimeSpan duration));
            Assert.Equal(TimeSpan.Zero, duration);
        }
    }
}
