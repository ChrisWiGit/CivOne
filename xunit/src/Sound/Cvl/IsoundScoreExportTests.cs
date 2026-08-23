using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Prüft den Weg CVL -> TuneScorePack -> *.score.json. Das JSON ist die Form, die
    /// CivOne ausliefert; ab da wird die CVL nicht mehr gebraucht.
    /// </summary>
    public class IsoundScoreExportTests
    {
        /// <summary>Setzen, um beim Testlauf die ausgelieferte Score-Datei neu zu erzeugen.</summary>
        private const string ScoreOutputVariable = "CIVONE_SCORE_OUT";

        private readonly ITestOutputHelper _output;

        public IsoundScoreExportTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Export_ProducesScorePack_WithTitlesAndKinds()
        {
            var pack = IsoundScoreExporter.Export(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl"));

            Assert.Equal("isound", pack.Id);
            Assert.Equal("ISOUND", pack.Driver);
            Assert.Equal("pcSpeaker", pack.Device);
            Assert.Equal(FakeIsoundModule.Signature, pack.SourceSignature);

            // 300 Hz Basis-Tick, Worker alle 5 Ticks -> 60 Hz, also 1/60 s pro Dauereinheit.
            Assert.Equal(60d, pack.WorkerTickHz);
            Assert.Equal(0.5d, pack.DurationSeconds(new TuneStep { Duration = 30 }), 6);

            var music = pack.Tunes.Single(t => t.TuneId == FakeIsoundModule.TuneMusicA);
            Assert.Equal("Title Music", music.Title);
            Assert.Equal(TuneScoreKind.Music, music.Kind);
            Assert.True(music.EndlessLoop);
            Assert.Equal(3, music.Steps.Count);
            Assert.Equal(54, music.TotalTicks);

            var silent = pack.Tunes.Single(t => t.TuneId == FakeIsoundModule.TuneSilent);
            Assert.Equal(TuneScoreKind.Silent, silent.Kind);
            Assert.Empty(silent.Steps);

            var effect = pack.Tunes.Single(t => t.TuneId == FakeIsoundModule.TuneEffect);
            Assert.Equal(TuneScoreKind.Effect, effect.Kind);

            // Handler ohne Sequenz werden standardmäßig weggelassen.
            Assert.DoesNotContain(pack.Tunes, t => t.TuneId == FakeIsoundModule.TuneUnsupported);
        }

        [Fact]
        public void Export_KeepsUnsupportedTunes_WhenRequested()
        {
            var options = new IsoundScoreOptions { SkipUnsupported = false };
            var pack = IsoundScoreExporter.Export(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl"), options);

            var unsupported = pack.Tunes.Single(t => t.TuneId == FakeIsoundModule.TuneUnsupported);
            Assert.Equal(TuneScoreKind.Unsupported, unsupported.Kind);
            Assert.Empty(unsupported.Steps);
        }

        [Fact]
        public void ScoreJson_RoundTripsWithoutLoss()
        {
            var pack = IsoundScoreExporter.Export(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl"));
            string path = Path.Combine(Path.GetTempPath(), $"isound-{Guid.NewGuid():N}.score.json");

            try
            {
                TuneScoreJson.Save(path, pack);
                var loaded = TuneScoreJson.Load(path);

                Assert.Equal(pack.Id, loaded.Id);
                Assert.Equal(pack.Driver, loaded.Driver);
                Assert.Equal(pack.FastTickHz, loaded.FastTickHz);
                Assert.Equal(pack.WorkerTickDivider, loaded.WorkerTickDivider);
                Assert.Equal(pack.Tunes.Count, loaded.Tunes.Count);
                Assert.Equal(TuneScoreJson.Serialize(pack), TuneScoreJson.Serialize(loaded));

                // Nach dem Laden hängt nichts mehr an der Quelldatei.
                var music = loaded.Tunes.Single(t => t.TuneId == FakeIsoundModule.TuneMusicA);
                Assert.Equal(8360, music.Steps[0].Divisor);
                Assert.Equal(SpeakerEffectKind.Vibrato, music.Steps[2].DecodedEffect.Kind);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void ScoreJson_RejectsSequenceWithoutSteps()
        {
            var pack = new TuneScorePack
            {
                Id = "broken",
                DisplayName = "Broken",
                Driver = "ISOUND",
                Device = "pcSpeaker",
                Tunes = [new TuneScore { TuneId = 3, Title = "Title Music", Kind = TuneScoreKind.Music }]
            };

            var error = Assert.Throws<InvalidOperationException>(() => TuneScoreJson.Serialize(pack));
            Assert.Contains("steps", error.Message);
        }

        [Fact]
        public void Export_RealIsound_WritesScoreJson()
        {
            string cvlPath = CvlTestFiles.TryFindIsound();
            if (cvlPath == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ISOUND.CVL", CvlTestFiles.IsoundEnvironmentVariable));
                return;
            }

            string configured = Environment.GetEnvironmentVariable(ScoreOutputVariable);
            string outputPath = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Path.GetTempPath(), $"isound-{Guid.NewGuid():N}.score.json")
                : configured;
            bool temporary = string.IsNullOrWhiteSpace(configured);

            try
            {
                IsoundScoreExporter.ExportToFile(cvlPath, outputPath);
                var loaded = TuneScoreJson.Load(outputPath);

                var withSteps = loaded.Tunes.Where(t => t.Steps.Count > 0).ToArray();
                Assert.True(withSteps.Length >= 20, $"Zu wenige Tunes extrahiert: {withSteps.Length}.");
                Assert.Contains(loaded.Tunes, t => t.TuneId == 34 && t.Kind == TuneScoreKind.Music);
                Assert.Contains(loaded.Tunes, t => t.TuneId == 4 && t.Kind == TuneScoreKind.Silent);

                _output.WriteLine($"{outputPath}: {loaded.Tunes.Count} Tunes, "
                                  + $"{loaded.Tunes.Sum(t => t.Steps.Count)} Schritte, "
                                  + $"{new FileInfo(outputPath).Length / 1024} KiB");
            }
            finally
            {
                if (temporary && File.Exists(outputPath)) File.Delete(outputPath);
            }
        }
    }
}
