using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Covers the ISOUND parser via a synthetic module (always runs) and additionally
    /// checks against the real ISOUND.CVL if it is available locally.
    /// </summary>
    public sealed class IsoundParserTests
    {
        private readonly ITestOutputHelper _output;

        public IsoundParserTests(ITestOutputHelper output) => _output = output;

        private static IsoundParser CreateFakeParser()
            => IsoundParser.Create(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl"));

        [Fact]
        public void CvlImageReadsModuleHeaderAndResolvesSegments()
        {
            var image = CvlImage.FromBytes(FakeIsoundModule.Build(), "fake-isound.cvl");

            Assert.Equal(FakeIsoundModule.ImageStart, image.ImageStart);
            Assert.Equal(FakeIsoundModule.Signature, image.Signature);
            Assert.Equal(0, image.CodeSegment);
            Assert.Equal(FakeIsoundModule.DataSegmentParagraphs, image.DataSegment);

            // Data-segment offsets count from DataStart, not from ImageStart - exactly the
            // point where the old converter got it wrong.
            Assert.Equal(image.ImageStart + FakeIsoundModule.DataSegmentParagraphs * 16, image.DataStart);
            Assert.NotEqual(image.ImageStart, image.DataStart);

            Assert.Equal(FakeIsoundModule.ExportCount, image.Exports.Count);
            Assert.Equal(FakeIsoundModule.PlayTune, image.Exports[CvlImage.ExportPlayTune]);
        }

        [Fact]
        public void CvlImageFromBytesRejectsNonMzData()
        {
            var bytes = new byte[512];
            var error = Assert.Throws<System.InvalidOperationException>(() => CvlImage.FromBytes(bytes, "broken.cvl"));
            Assert.Contains("MZ", error.Message);
        }

        [Fact]
        public void TryCreateDerivesLayoutFromModuleCode()
        {
            var parser = CreateFakeParser();
            var layout = parser.Layout;

            Assert.Equal(FakeIsoundModule.DispatchTable, layout.DispatchTable);
            Assert.Equal(FakeIsoundModule.MaxTuneId, layout.MaxTuneId);
            Assert.Equal(FakeIsoundModule.MusicPlayer, layout.MusicPlayer);
            Assert.Equal(FakeIsoundModule.EffectPlayer, layout.EffectPlayer);
            Assert.Equal(FakeIsoundModule.EffectParamTable, layout.EffectParamTable);
            Assert.Equal(FakeIsoundModule.PlainTimbreCode, layout.PlainTimbreCode);
            Assert.Equal(FakeIsoundModule.FirstTimbreCode, layout.FirstTimbreCode);
        }

        [Fact]
        public void TryCreateFailsWithReasonWhenModuleIsNotIsound()
        {
            // Valid CVL header, but without PlayTune dispatch.
            var bytes = FakeIsoundModule.Build();
            for (int i = 0; i < 64; i++) bytes[FakeIsoundModule.ImageStart + FakeIsoundModule.PlayTune + i] = 0x90;

            var image = CvlImage.FromBytes(bytes, "not-isound.cvl");

            Assert.False(IsoundParser.TryCreate(image, out var parser, out string? error));
            Assert.Null(parser);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void ParseTuneMusicReadsFourByteRecords()
        {
            var info = CreateFakeParser().ParseTune(FakeIsoundModule.TuneMusicA);

            Assert.Equal(TuneScoreKind.Music, info.Kind);
            Assert.Equal(FakeIsoundModule.MusicHandlerA, info.HandlerOffset);
            Assert.Equal(FakeIsoundModule.MusicDataA, info.DataOffset);
            Assert.Equal(3, info.Steps.Count);

            var note = info.Steps[0];
            Assert.Equal(22, note.Duration);
            Assert.Equal(8360, note.Divisor);
            Assert.Equal(0x7E, note.Timbre);
            Assert.Equal(1, note.NoiseMask);
            Assert.False(note.IsRest);
        }

        [Fact]
        public void ParseTuneMusicRestHasDurationButNoTone()
        {
            var rest = CreateFakeParser().ParseTune(FakeIsoundModule.TuneMusicA).Steps[1];

            Assert.True(rest.IsRest);
            Assert.Equal(2, rest.Duration);
            Assert.Equal(0, rest.Divisor);
            Assert.Equal(0, rest.Effect);
        }

        [Fact]
        public void ParseTuneMusicResolvesEffectFromTimbreTable()
        {
            var parser = CreateFakeParser();

            // Timbre 0x69 -> table index 4.
            var note = parser.ParseTune(FakeIsoundModule.TuneMusicA).Steps[2];
            Assert.Equal(0x69, note.Timbre);
            Assert.Equal(FakeIsoundModule.EffectParams[4], note.Effect);

            var vibrato = note.DecodedEffect;
            Assert.Equal(SpeakerEffectKind.Vibrato, vibrato.Kind);
            Assert.Equal(0x04, vibrato.Range);
            Assert.Equal(0x02, vibrato.Step);

            // Timbre 0x7E is the special case "plain tone".
            Assert.Equal(0, parser.ResolveMusicEffect(FakeIsoundModule.PlainTimbreCode));

            // Codes below the start of the table also have no effect.
            Assert.Equal(0, parser.ResolveMusicEffect(0x62));
        }

        [Fact]
        public void ParseTuneSilentWhenHandlerReturnsImmediately()
        {
            var info = CreateFakeParser().ParseTune(FakeIsoundModule.TuneSilent);

            Assert.Equal(TuneScoreKind.Silent, info.Kind);
            Assert.Empty(info.Steps);
            Assert.Null(info.Diagnostic);
        }

        [Fact]
        public void ParseTuneEffectReadsTenByteRecordsAndShortRest()
        {
            var info = CreateFakeParser().ParseTune(FakeIsoundModule.TuneEffect);

            Assert.Equal(TuneScoreKind.Effect, info.Kind);
            Assert.Equal(3, info.Steps.Count);

            Assert.Equal(7, info.Steps[0].Duration);
            Assert.Equal(0x0898, info.Steps[0].Divisor);
            Assert.Equal(0x0FFF, info.Steps[0].NoiseMask);
            Assert.Equal(SpeakerEffectKind.Slide, info.Steps[0].DecodedEffect.Kind);
            Assert.Equal(0x14, info.Steps[0].DecodedEffect.Delta);

            // A mask of 0 shortens the record to 6 bytes and turns the speaker off.
            Assert.True(info.Steps[1].IsRest);
            Assert.Equal(2, info.Steps[1].Duration);
            Assert.Equal(0, info.Steps[1].NoiseMask);

            Assert.Equal(90, info.Steps[2].Duration);
            Assert.Equal(0x07D0, info.Steps[2].Divisor);
        }

        [Fact]
        public void ParseTuneUnsupportedWhenHandlerIsNotASequence()
        {
            var info = CreateFakeParser().ParseTune(FakeIsoundModule.TuneUnsupported);

            Assert.Equal(TuneScoreKind.Unsupported, info.Kind);
            Assert.Empty(info.Steps);
            Assert.False(string.IsNullOrWhiteSpace(info.Diagnostic));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(0x2D)]
        [InlineData(-1)]
        public void ParseTuneUnsupportedWhenDispatchEntryIsEmptyOrOutOfRange(int tuneId)
        {
            // 3 is taken, so only the edge cases need checking.
            var info = CreateFakeParser().ParseTune(tuneId);

            if (tuneId == FakeIsoundModule.TuneMusicA)
            {
                Assert.Equal(TuneScoreKind.Music, info.Kind);
                return;
            }

            Assert.Equal(TuneScoreKind.Unsupported, info.Kind);
            Assert.False(string.IsNullOrWhiteSpace(info.Diagnostic));
        }

        // The enum type is internal, so the expected value is passed as its name.
        [Theory]
        [InlineData(0x0000, nameof(SpeakerEffectKind.None), 0, 0, 0)]
        [InlineData(0x8306, nameof(SpeakerEffectKind.Vibrato), 0x06, 0x03, 0)]
        [InlineData(0x8FFF, nameof(SpeakerEffectKind.Vibrato), 0xFF, 0x0F, 0)]
        [InlineData(0x000F, nameof(SpeakerEffectKind.Slide), 0, 0, 15)]
        [InlineData(0xFFF1, nameof(SpeakerEffectKind.Slide), 0, 0, -15)]
        public void SpeakerEffectDecodeSplitsVibratoAndSlide(int raw, string kind, int range, int step, int delta)
        {
            var effect = SpeakerEffect.Decode(raw);

            Assert.Equal(kind, effect.Kind.ToString());
            Assert.Equal(range, effect.Range);
            Assert.Equal(step, effect.Step);
            Assert.Equal(delta, effect.Delta);
        }

        [Fact]
        public void FrequencyHzUsesPitClockDividedByDivisor()
        {
            var step = new TuneStep { Duration = 1, Divisor = 4180 };

            Assert.Equal(285.45d, step.FrequencyHz(1_193_182), 2);
            Assert.Equal(0d, new TuneStep { Duration = 1, Divisor = 0 }.FrequencyHz(1_193_182));
        }

        // ---------------------------------------------------------------------------------
        // Integration tests against the real ISOUND.CVL. Opt-in: nothing happens without the file.
        // ---------------------------------------------------------------------------------

        private const string KnownBuildSignature = "Civil IBM   11-14-91";

        private static readonly int[] DistinctSequenceTuneIds = [3, 5, 9, 34, 35];

        private IsoundParser? TryCreateRealParser(out CvlImage? image)
        {
            image = null;
            string? path = CvlTestFiles.TryFindIsound();
            if (path == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ISOUND.CVL", CvlTestFiles.IsoundEnvironmentVariable));
                return null;
            }

            image = CvlImage.Load(path);
            return IsoundParser.Create(image);
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundLayoutMatchesKnownBuild()
        {
            var parser = TryCreateRealParser(out var image);
            if (parser == null || image == null) return;

            _output.WriteLine($"Signature: {image.Signature}");
            _output.WriteLine($"Dispatch 0x{parser.Layout.DispatchTable:X4}, Music 0x{parser.Layout.MusicPlayer:X4}, "
                              + $"Effect 0x{parser.Layout.EffectPlayer:X4}, Effect table 0x{parser.Layout.EffectParamTable:X4}");

            Assert.Equal(0x2C, parser.Layout.MaxTuneId);
            Assert.True(parser.Layout.MusicPlayer > 0);
            Assert.True(parser.Layout.EffectPlayer > 0);
            Assert.NotEqual(parser.Layout.MusicPlayer, parser.Layout.EffectPlayer);

            if (image.Signature != KnownBuildSignature) return;

            Assert.Equal(0x0588, parser.Layout.DispatchTable);
            Assert.Equal(0x04BB, parser.Layout.MusicPlayer);
            Assert.Equal(0x0423, parser.Layout.EffectPlayer);
            Assert.Equal(0x04A5, parser.Layout.EffectParamTable);
            Assert.Equal(0x7E, parser.Layout.PlainTimbreCode);
            Assert.Equal(0x65, parser.Layout.FirstTimbreCode);
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundWinMusicStartsWithExpectedNotesOnAUniformGrid()
        {
            var parser = TryCreateRealParser(out _);
            if (parser == null) return;

            var info = parser.ParseTune(34);
            Assert.Equal(TuneScoreKind.Music, info.Kind);
            Assert.True(info.Steps.Count > 20, "Win Music should have significantly more than 20 steps.");

            // First half-bar: tone, short rest, tone, short rest.
            Assert.Equal(27, info.Steps[0].Duration);
            Assert.Equal(3320, info.Steps[0].Divisor);
            Assert.Equal(0x68, info.Steps[0].Timbre);
            Assert.True(info.Steps[1].IsRest);
            Assert.Equal(3, info.Steps[1].Duration);
            Assert.Equal(29, info.Steps[2].Duration);
            Assert.Equal(3320, info.Steps[2].Divisor);

            // Tone + rest each add up to 30 worker ticks - at 60 Hz that's exactly half a second.
            var groups = GroupNoteAndRest(info.Steps).Take(12).ToArray();
            Assert.Equal(12, groups.Length);
            Assert.All(groups, ticks => Assert.Equal(30, ticks));
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundTune4IsSilentByDesign()
        {
            var parser = TryCreateRealParser(out _);
            if (parser == null) return;

            var info = parser.ParseTune(4);

            Assert.Equal(TuneScoreKind.Silent, info.Kind);
            Assert.Empty(info.Steps);
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundTunesHaveDistinctSequences()
        {
            var parser = TryCreateRealParser(out _);
            if (parser == null) return;

            var sequences = DistinctSequenceTuneIds
                .Select(parser.ParseTune)
                .ToArray();

            Assert.All(sequences, info => Assert.Equal(TuneScoreKind.Music, info.Kind));
            Assert.All(sequences, info => Assert.NotEmpty(info.Steps));

            var fingerprints = sequences
                .Select(info => string.Join("|", info.Steps.Select(s => $"{s.Timbre}:{s.Duration}:{s.Divisor}")))
                .ToArray();

            Assert.Equal(fingerprints.Length, fingerprints.Distinct().Count());
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundAllParsedTunesStayInPlausibleRanges()
        {
            var parser = TryCreateRealParser(out _);
            if (parser == null) return;

            var parsed = CvlTuneCatalog.PlayableTuneIds
                .Select(parser.ParseTune)
                .Where(info => info.Kind is TuneScoreKind.Music or TuneScoreKind.Effect)
                .ToArray();

            Assert.True(parsed.Length >= 20, $"Significantly more tunes should be readable, found: {parsed.Length}.");

            foreach (var info in parsed)
            {
                Assert.NotEmpty(info.Steps);
                Assert.All(info.Steps, step =>
                {
                    Assert.InRange(step.Duration, 1, 0xFFFF);
                    Assert.InRange(step.Divisor, 0, 0xFFFF);

                    // Everything audible lies between roughly 30 Hz and 5 kHz.
                    if (!step.IsRest) Assert.InRange(step.FrequencyHz(1_193_182), 30d, 5000d);
                });

                if (info.Kind != TuneScoreKind.Music) continue;

                // Music records only use a narrow range of timbre codes: 0x62 for rests,
                // 0x65..0x6F for the effect table, and 0x7E for the plain tone.
                // A parser shifted by bytes would read divisor bytes here, i.e. arbitrary values.
                Assert.All(info.Steps, step =>
                    Assert.InRange(step.Timbre, 0x60, parser.Layout.PlainTimbreCode));
            }
        }

        private static IEnumerable<int> GroupNoteAndRest(IReadOnlyList<TuneStep> steps)
        {
            int accumulated = 0;
            foreach (var step in steps)
            {
                accumulated += step.Duration;
                if (!step.IsRest) continue;

                yield return accumulated;
                accumulated = 0;
            }
        }
    }
}
