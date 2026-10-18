using System.Collections.Generic;
using CivOne.Sound;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers the fall-back wave file names, which keep a collection of wave files from an older
    /// CivOne working after the sounds were renamed.
    /// </summary>
    public sealed class LegacyWaveNameTests
    {
        private readonly LegacyWaveNameDelegate _delegateUnderTest = new();

        /// <summary>
        /// The sounds that had a wave file before still find it under its old name.
        /// </summary>
        [Theory]
        [InlineData(SoundNames.MusicTitle, "opening")]
        [InlineData(SoundNames.MusicEvolution, "evolution")]
        [InlineData(SoundNames.MusicLose, "lose2")]
        [InlineData(SoundNames.LeaderLincoln, "linc")]
        [InlineData(SoundNames.LeaderLincolnShort, "linc_short")]
        [InlineData(SoundNames.UiBeep, "s_beep")]
        [InlineData(SoundNames.EventNuclearBlast, "s_nuke")]
        [InlineData(SoundNames.CombatAirStrike, "airnuke")]
        public void AKnownSoundKeepsItsOldFileName(string soundName, string expected)
        {
            Assert.Contains(expected, _delegateUnderTest.Candidates(soundName));
        }

        /// <summary>
        /// The win music was called two different things over time, so both are offered.
        /// </summary>
        [Fact]
        public void TheWinMusicOffersBothOfItsOldNames()
        {
            Assert.Equal(["wintune", "win"], _delegateUnderTest.Candidates(SoundNames.MusicWin));
        }

        /// <summary>
        /// The combat outcomes are an approximation: the older scheme picked its file by unit type
        /// and used the same one whether that unit won or lost, so the shared file comes first and
        /// the win/loss distinction second.
        /// </summary>
        [Theory]
        [InlineData(SoundNames.CombatWinWeak, new[] { "s_land", "they_die" })]
        [InlineData(SoundNames.CombatLossWeak, new[] { "s_land", "we_die" })]
        [InlineData(SoundNames.CombatWinStrong, new[] { "they_die" })]
        [InlineData(SoundNames.CombatLossStrong, new[] { "we_die" })]
        public void TheCombatOutcomesFallBackToTheOldCombatFiles(string soundName, string[] expected)
        {
            Assert.Equal(expected, _delegateUnderTest.Candidates(soundName));
        }

        /// <summary>
        /// A name a plugin brings never had a wave file, so there is nothing to fall back to and the
        /// lookup simply ends.
        /// </summary>
        [Fact]
        public void AnUnknownSoundHasNoFallback()
        {
            Assert.Empty(_delegateUnderTest.Candidates("some_plugin_sound"));
        }

        /// <summary>
        /// A fall-back name is a file name too, so it has to survive being written to disk.
        /// </summary>
        [Fact]
        public void EveryFallbackNameIsUsableAsAFileName()
        {
            foreach (CivOne.Sound.Cvl.CvlTuneDefinition tune in CivOne.Sound.Cvl.CvlTuneCatalog.Tunes)
            {
                foreach (string candidate in _delegateUnderTest.Candidates(tune.Name))
                {
                    Assert.Matches("^[a-z0-9_]+$", candidate);
                }
            }
        }

        /// <summary>
        /// Two sounds must not both fall back to a file that only one of them should get, except
        /// where the older scheme really did share one file.
        /// </summary>
        [Fact]
        public void OnlyTheSharedCombatFilesAreOfferedTwice()
        {
            var counts = new Dictionary<string, int>();

            foreach (CivOne.Sound.Cvl.CvlTuneDefinition tune in CivOne.Sound.Cvl.CvlTuneCatalog.Tunes)
            {
                foreach (string candidate in _delegateUnderTest.Candidates(tune.Name))
                {
                    counts[candidate] = counts.TryGetValue(candidate, out int count) ? count + 1 : 1;
                }
            }

            var shared = new List<string>();
            foreach (KeyValuePair<string, int> entry in counts)
            {
                if (entry.Value > 1) shared.Add(entry.Key);
            }

            shared.Sort(System.StringComparer.Ordinal);

            Assert.Equal(["s_land", "they_die", "we_die"], shared);
        }
    }
}
