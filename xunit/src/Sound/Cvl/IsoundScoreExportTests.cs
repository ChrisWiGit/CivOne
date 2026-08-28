using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Checks the path CVL -> TuneScore -> *.sound.json. The JSON is the form CivOne ships;
    /// the CVL is no longer needed from that point on.
    ///
    /// The pack's header data (driver, device, tick rates) deliberately does not live in the
    /// tune file, only in index.json - see <see cref="CvlSoundConversionServiceTests"/>.
    /// </summary>
    public sealed class IsoundScoreExportTests
    {
        private readonly ITestOutputHelper _output;

        public IsoundScoreExportTests(ITestOutputHelper output) => _output = output;

        private static List<TuneScore> Export(IsoundScoreOptions? options = null)
            => IsoundScoreExporter.Export(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl"), options);

        [Fact]
        public void ExportProducesTunesWithTitlesAndKinds()
        {
            List<TuneScore> tunes = Export();

            var music = tunes.Single(t => t.TuneId == FakeIsoundModule.TuneMusicA);
            Assert.Equal("Title Music", music.Title);
            Assert.Equal(TuneScoreKind.Music, music.Kind);
            Assert.True(music.EndlessLoop);
            Assert.Equal(3, music.Steps.Count);
            Assert.Equal(54, music.TotalTicks);
            Assert.Equal(SoundPackIndex.CurrentSchemaVersion, music.SchemaVersion);

            var silent = tunes.Single(t => t.TuneId == FakeIsoundModule.TuneSilent);
            Assert.Equal(TuneScoreKind.Silent, silent.Kind);
            Assert.Empty(silent.Steps);

            var effect = tunes.Single(t => t.TuneId == FakeIsoundModule.TuneEffect);
            Assert.Equal(TuneScoreKind.Effect, effect.Kind);

            // Handlers without a sequence are skipped by default.
            Assert.DoesNotContain(tunes, t => t.TuneId == FakeIsoundModule.TuneUnsupported);
        }

        [Fact]
        public void TimingConstantsDescribeTheSchedulerOfTheOriginal()
        {
            // 300 Hz base tick, sequencer every 5 ticks -> 60 Hz, i.e. 1/60 s per duration unit.
            Assert.Equal(300, IsoundScoreExporter.FastTickHz);
            Assert.Equal(5, IsoundScoreExporter.WorkerTickDivider);
            Assert.Equal(1_193_182, IsoundScoreExporter.PitClockHz);

            double sequencerHz = IsoundScoreExporter.FastTickHz / (double)IsoundScoreExporter.WorkerTickDivider;
            Assert.Equal(60d, sequencerHz);
            Assert.Equal(0.5d, 30 / sequencerHz, 6);
        }

        [Fact]
        public void ExportKeepsUnsupportedTunesWhenRequested()
        {
            List<TuneScore> tunes = Export(new IsoundScoreOptions { SkipUnsupported = false });

            var unsupported = tunes.Single(t => t.TuneId == FakeIsoundModule.TuneUnsupported);
            Assert.Equal(TuneScoreKind.Unsupported, unsupported.Kind);
            Assert.Empty(unsupported.Steps);
        }

        [Fact]
        public void ScoreJsonRoundTripsWithoutLoss()
        {
            TuneScore music = Export().Single(t => t.TuneId == FakeIsoundModule.TuneMusicA);
            string path = Path.Combine(Path.GetTempPath(), $"isound-{Guid.NewGuid():N}.sound.json");

            try
            {
                TuneScoreJson.Save(path, music);
                TuneScore loaded = TuneScoreJson.Load(path);

                Assert.Equal(music.TuneId, loaded.TuneId);
                Assert.Equal(music.Title, loaded.Title);
                Assert.Equal(music.Steps.Count, loaded.Steps.Count);
                Assert.Equal(TuneScoreJson.Serialize(music), TuneScoreJson.Serialize(loaded));

                // After loading, nothing depends on the source file anymore.
                Assert.Equal(8360, loaded.Steps[0].Divisor);
                Assert.Equal(SpeakerEffectKind.Vibrato, loaded.Steps[2].DecodedEffect.Kind);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void ScoreJsonRejectsSequenceWithoutSteps()
        {
            var broken = new TuneScore { TuneId = 3, Title = "Title Music", Kind = TuneScoreKind.Music };

            var error = Assert.Throws<InvalidOperationException>(() => TuneScoreJson.Serialize(broken));
            Assert.Contains("steps", error.Message);
        }

        [Fact]
        public void ScoreJsonRejectsAFileFromAnOlderPackLayout()
        {
            TuneScore music = Export().Single(t => t.TuneId == FakeIsoundModule.TuneMusicA);
            music.SchemaVersion = 1;

            var error = Assert.Throws<InvalidOperationException>(() => TuneScoreJson.Serialize(music));
            Assert.Contains("schemaVersion", error.Message);
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void ExportRealIsoundYieldsTheExpectedTunes()
        {
            string? cvlPath = CvlTestFiles.TryFindIsound();
            if (cvlPath == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ISOUND.CVL", CvlTestFiles.IsoundEnvironmentVariable));
                return;
            }

            List<TuneScore> tunes = IsoundScoreExporter.ExportFromFile(cvlPath);

            var withSteps = tunes.Where(t => t.Steps.Count > 0).ToArray();
            Assert.True(withSteps.Length >= 20, $"Too few tunes extracted: {withSteps.Length}.");
            Assert.Contains(tunes, t => t.TuneId == 34 && t.Kind == TuneScoreKind.Music);
            Assert.Contains(tunes, t => t.TuneId == 4 && t.Kind == TuneScoreKind.Silent);

            _output.WriteLine($"{tunes.Count} tunes, {tunes.Sum(t => t.Steps.Count)} steps");
        }
    }
}
