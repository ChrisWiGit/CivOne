using System;
using System.Linq;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Covers the table that turns a CVL tune number into the name the game plays it by.
    /// </summary>
    public sealed class CvlTuneCatalogTests
    {
        /// <summary>
        /// Two tunes sharing a number or a name would make the lookups ambiguous and, for the name,
        /// would put two tunes in the same file.
        /// </summary>
        [Fact]
        public void TuneNumbersAndNamesAreUnique()
        {
            Assert.Equal(CvlTuneCatalog.Tunes.Count, CvlTuneCatalog.Tunes.Select(t => t.TuneId).Distinct().Count());
            Assert.Equal(
                CvlTuneCatalog.Tunes.Count,
                CvlTuneCatalog.Tunes.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        /// <summary>
        /// A number outside the range the drivers accept could never be played.
        /// </summary>
        [Fact]
        public void EveryTuneNumberIsPlayable()
        {
            foreach (CvlTuneDefinition tune in CvlTuneCatalog.Tunes)
            {
                Assert.InRange(tune.TuneId, CvlTuneCatalog.FirstPlayableTuneId, CvlTuneCatalog.LastPlayableTuneId);
            }
        }

        /// <summary>
        /// Both lookups have to find the same entry, otherwise a pack written by number would be
        /// read back under a different name.
        /// </summary>
        [Fact]
        public void LookupByNumberAndByNameAgree()
        {
            foreach (CvlTuneDefinition tune in CvlTuneCatalog.Tunes)
            {
                Assert.Same(tune, CvlTuneCatalog.Find(tune.TuneId));
                Assert.Same(tune, CvlTuneCatalog.Find(tune.Name));
                Assert.Equal(tune.Name, CvlTuneCatalog.ResolveName(tune.TuneId));
                Assert.Equal(tune.Title, CvlTuneCatalog.ResolveTitle(tune.TuneId));
            }
        }

        /// <summary>
        /// A pack may hold a tune we have not identified. It still needs a name, and that name has
        /// to carry the number so the tune stays traceable back to the module.
        /// </summary>
        [Fact]
        public void AnUnidentifiedTuneGetsAGeneratedName()
        {
            const int unknownTuneId = 99;

            Assert.Null(CvlTuneCatalog.Find(unknownTuneId));
            Assert.Equal("tune_99", CvlTuneCatalog.ResolveName(unknownTuneId));
            Assert.Equal("Tune 99", CvlTuneCatalog.ResolveTitle(unknownTuneId));
        }

        /// <summary>
        /// The warm-up walks this list to decide what to render first, so it has to cover the whole
        /// catalog and mention nothing twice.
        /// </summary>
        [Fact]
        public void TheWarmUpOrderCoversEveryTuneOnce()
        {
            Assert.Equal(CvlTuneCatalog.Tunes.Count, CvlTuneCatalog.WarmUpOrder.Count);
            Assert.Equal(
                CvlTuneCatalog.Tunes.Select(t => t.Name).OrderBy(name => name, StringComparer.Ordinal),
                CvlTuneCatalog.WarmUpOrder.OrderBy(name => name, StringComparer.Ordinal));
        }

        /// <summary>
        /// The music the game needs first is rendered first; the rest follows.
        /// </summary>
        [Fact]
        public void TheWarmUpOrderStartsWithTheLongMusic()
        {
            Assert.Equal(
                [SoundNames.MusicTitle, SoundNames.MusicEvolution, SoundNames.MusicWin, SoundNames.MusicLose],
                CvlTuneCatalog.WarmUpOrder.Take(4));
        }

        /// <summary>
        /// Only the title and evolution music repeat; everything else has to end on its own.
        /// </summary>
        [Fact]
        public void OnlyTheTitleAndEvolutionMusicLoop()
        {
            Assert.True(CvlTuneCatalog.IsEndlessLoop(3));
            Assert.True(CvlTuneCatalog.IsEndlessLoop(4));

            foreach (CvlTuneDefinition tune in CvlTuneCatalog.Tunes.Where(t => t.TuneId is not (3 or 4)))
            {
                Assert.False(tune.EndlessLoop, $"Tune {tune.TuneId} should not loop.");
            }
        }
    }
}
