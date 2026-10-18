using System.Threading.Tasks;
using CivOne;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers that exactly one sound source is built for a setting, with nothing behind it to fall
    /// back to.
    /// </summary>
    public sealed class SoundPlaybackStrategyFactoryTests
    {
        private readonly SoundPackPlaybackService _service;

        /// <summary>Renders nothing; the factory never gets as far as playing a tune.</summary>
        private sealed class SilentRenderQueue : ISoundPackRenderQueue
        {
            public string? TryGetCached(string packFolder, string fileName, int arrangement) => null;

            public Task<string?> Request(string packFolder, string fileName, int arrangement)
                => Task.FromResult<string?>(null);

            public void WarmPack(string packFolder) { }
        }

        /// <summary>Registers a runtime, which the pack strategy needs to play through.</summary>
        public SoundPlaybackStrategyFactoryTests()
            => _service = new SoundPackPlaybackService(new MockRuntime(new RuntimeSettings()), new SilentRenderQueue());

        /// <summary>Nothing selected plays nothing, rather than quietly picking a source.</summary>
        [Fact]
        public void AnEmptySettingPlaysNothing()
            => Assert.IsType<NoSoundPlaybackStrategy>(SoundPlaybackStrategyFactory.Create(string.Empty, _service));

        /// <summary>Sound switched off plays nothing.</summary>
        [Fact]
        public void NonePlaysNothing()
            => Assert.IsType<NoSoundPlaybackStrategy>(
                SoundPlaybackStrategyFactory.Create(SoundPlaybackStrategyConstants.NoSoundPack, _service));

        /// <summary>The wave setting plays wave files and nothing else.</summary>
        [Fact]
        public void WaveFilesPlayWaveFiles()
            => Assert.IsType<WaveSoundPlaybackStrategy>(
                SoundPlaybackStrategyFactory.Create(SoundPlaybackStrategyConstants.WaveSoundPack, _service));

        /// <summary>
        /// A pack plays that pack. There is no wrapper behind it, so a sound the pack does not carry
        /// is silent instead of being taken from the wave files.
        /// </summary>
        [Fact]
        public void APackPlaysThatPackAlone()
            => Assert.IsType<SoundPackPlaybackStrategy>(SoundPlaybackStrategyFactory.Create("pc-speaker", _service));

        /// <summary>
        /// Redirects sit in front of the chosen source, so they can still move a sound between
        /// sources even though the source itself never falls back.
        /// </summary>
        [Fact]
        public void RedirectsWrapTheChosenSource()
        {
            ISoundPlaybackStrategy strategy = SoundPlaybackStrategyFactory.Create(
                SoundPlaybackStrategyConstants.WaveSoundPack, _service, new SoundAliasRegistry());

            Assert.IsType<AliasSoundPlaybackStrategy>(strategy);
        }
    }
}
