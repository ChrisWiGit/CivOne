using System.Collections.Generic;
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
        /// Builds an index with the given tunes and name mapping.
        /// </summary>
        /// <param name="tunes">Tune id and file name; a <c>null</c> file marks a silent tune.</param>
        /// <param name="soundNames">Engine sound name to tune id.</param>
        /// <returns>The index.</returns>
        private static SoundPackIndex Index((int TuneId, string? File)[] tunes,
            params (string Name, int TuneId)[] soundNames)
        {
            var index = new SoundPackIndex
            {
                PackId = "test",
                DisplayName = "Test",
                Driver = "test",
                Device = "pcSpeaker"
            };

            foreach ((int tuneId, string? file) in tunes)
            {
                index.Tunes.Add(new SoundPackIndexEntry { TuneId = tuneId, Title = $"Tune {tuneId}", File = file });
            }

            foreach ((string name, int tuneId) in soundNames)
            {
                index.SoundNames[name] = tuneId;
            }

            return index;
        }

        /// <summary>
        /// The sounds the game asks for first come first, in the order of the engine's own name list.
        /// </summary>
        [Fact]
        public void MappedSoundsComeFirstInEngineOrder()
        {
            SoundPackIndex index = Index(
                [(3, "opening.json"), (34, "win.json"), (35, "lose.json")],
                ("lose2", 35), ("opening", 3), ("wintune", 34));

            IReadOnlyList<string> order = _delegateUnderTest.Order(index);

            Assert.Equal(["opening.json", "win.json", "lose.json"], order);
        }

        /// <summary>
        /// Tunes without a mapped name are still rendered, just after the mapped ones.
        /// </summary>
        [Fact]
        public void UnmappedTunesFollowTheMappedOnes()
        {
            SoundPackIndex index = Index(
                [(20, "effect.json"), (3, "opening.json"), (21, "other.json")],
                ("opening", 3));

            IReadOnlyList<string> order = _delegateUnderTest.Order(index);

            Assert.Equal(["opening.json", "effect.json", "other.json"], order);
        }

        /// <summary>
        /// A tune that is deliberately silent has no file, so there is nothing to render for it.
        /// </summary>
        [Fact]
        public void SilentTunesAreLeftOut()
        {
            SoundPackIndex index = Index([(3, "opening.json"), (4, null)], ("opening", 3));

            Assert.Equal(["opening.json"], _delegateUnderTest.Order(index));
        }

        /// <summary>
        /// A tune reachable through more than one name is rendered once, not once per name.
        /// </summary>
        [Fact]
        public void ATuneIsListedOnlyOnce()
        {
            SoundPackIndex index = Index(
                [(3, "shared.json")],
                ("opening", 3), ("wintune", 3));

            Assert.Equal(["shared.json"], _delegateUnderTest.Order(index));
        }

        /// <summary>
        /// A name that points at a tune the pack does not have is skipped instead of throwing.
        /// </summary>
        [Fact]
        public void UnknownTuneIdsAreIgnored()
        {
            SoundPackIndex index = Index([(3, "opening.json")], ("opening", 3), ("wintune", 99));

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
