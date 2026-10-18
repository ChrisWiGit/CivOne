using System.Collections.Generic;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers the order in which a pack's tunes are pre-rendered.
    /// </summary>
    public sealed class SoundPackWarmUpOrderTests
    {
        private readonly SoundPackWarmUpOrderDelegate _delegateUnderTest = new();

        /// <summary>
        /// Builds an index with the given tunes.
        /// </summary>
        /// <param name="tunes">Sound name and file name; a <c>null</c> file marks a silent tune.</param>
        /// <returns>The index.</returns>
        private static SoundPackIndex Index(params (string Name, string? File)[] tunes)
        {
            var index = new SoundPackIndex
            {
                PackId = "test",
                DisplayName = "Test",
                Driver = "test",
                Device = "pcSpeaker"
            };

            foreach ((string name, string? file) in tunes)
            {
                index.Tunes.Add(new SoundPackIndexEntry { Name = name, Title = name, File = file });
            }

            return index;
        }

        /// <summary>
        /// Tunes the catalog knows come first, in the catalog's warm-up order rather than in the
        /// order the pack happens to list them.
        /// </summary>
        [Fact]
        public void KnownSoundsComeFirstInCatalogOrder()
        {
            SoundPackIndex index = Index(
                (SoundNames.MusicLose, "lose.json"),
                (SoundNames.MusicTitle, "opening.json"),
                (SoundNames.MusicWin, "win.json"));

            IReadOnlyList<string> order = _delegateUnderTest.Order(index);

            Assert.Equal(["opening.json", "win.json", "lose.json"], order);
        }

        /// <summary>
        /// Tunes the catalog does not name are still rendered, just after the known ones.
        /// </summary>
        [Fact]
        public void UnknownTunesFollowTheKnownOnes()
        {
            SoundPackIndex index = Index(
                ("tune_20", "effect.json"),
                (SoundNames.MusicTitle, "opening.json"),
                ("tune_21", "other.json"));

            IReadOnlyList<string> order = _delegateUnderTest.Order(index);

            Assert.Equal(["opening.json", "effect.json", "other.json"], order);
        }

        /// <summary>
        /// A tune that is deliberately silent has no file, so there is nothing to render for it.
        /// </summary>
        [Fact]
        public void SilentTunesAreLeftOut()
        {
            SoundPackIndex index = Index(
                (SoundNames.MusicTitle, "opening.json"),
                (SoundNames.MusicEvolution, null));

            Assert.Equal(["opening.json"], _delegateUnderTest.Order(index));
        }

        /// <summary>
        /// Two names sharing one file render it once, not once per name.
        /// </summary>
        [Fact]
        public void AFileIsListedOnlyOnce()
        {
            SoundPackIndex index = Index(
                (SoundNames.MusicTitle, "shared.json"),
                (SoundNames.MusicWin, "shared.json"));

            Assert.Equal(["shared.json"], _delegateUnderTest.Order(index));
        }

        /// <summary>
        /// A name the catalog knows but the pack has no tune for is skipped instead of throwing.
        /// </summary>
        [Fact]
        public void NamesWithoutATuneAreIgnored()
        {
            SoundPackIndex index = Index((SoundNames.MusicTitle, "opening.json"));

            Assert.Equal(["opening.json"], _delegateUnderTest.Order(index));
        }

        /// <summary>
        /// A folder without an index yields nothing rather than failing the warm-up.
        /// </summary>
        [Fact]
        public void AFolderWithoutAnIndexYieldsNothing()
        {
            Assert.Empty(_delegateUnderTest.Order(System.IO.Path.GetTempPath()));
        }
    }
}
