using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl.Adlib
{
    /// <summary>
    /// Checks the ASOUND parser against the real driver, if it is available locally.
    ///
    /// The file belongs to the original game and is deliberately not in the repository, so these
    /// tests skip themselves when it is missing.
    /// </summary>
    public sealed class AsoundRealModuleTests
    {
        private readonly ITestOutputHelper _output;

        public AsoundRealModuleTests(ITestOutputHelper output) => _output = output;

        private AsoundParser? TryCreateParser()
        {
            string? path = CvlTestFiles.TryFindAsound();
            if (path == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ASOUND.CVL", CvlTestFiles.AsoundEnvironmentVariable));
                return null;
            }

            CvlImage image = CvlImage.Load(path);
            Assert.Equal(CvlDevice.AdLib, CvlDeviceDetector.Detect(image));

            Assert.True(AsoundParser.TryCreate(image, out AsoundParser? parser, out string? error), error);
            return parser;
        }

        [Fact]
        public void LayoutIsDerivedFromTheModule()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            AsoundLayout layout = parser.Layout;
            _output.WriteLine($"dispatch=0x{layout.DispatchTable:X4} maxTuneId={layout.MaxTuneId} "
                              + $"voices={layout.VoiceCount} bank=0x{layout.InstrumentBank:X4}/{layout.InstrumentStride} "
                              + $"ops=0x{layout.ChannelOperatorTable:X4}/0x{layout.OperatorRegisterTable:X4} "
                              + $"fnum=0x{layout.FrequencyNumberTable:X4} pan=0x{layout.DefaultPan:X2}");

            Assert.Equal(44, layout.MaxTuneId);
            Assert.Equal(9, layout.VoiceCount);
            Assert.Equal(44, layout.InstrumentStride);
            Assert.Equal(22, layout.OperatorStride);
            Assert.Equal(0x40, layout.DefaultPan);
        }

        [Fact]
        public void OperatorTablesMatchTheStandardOplChannelLayout()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            (int Modulator, int Carrier)[] operators = parser.ReadChannelOperators();

            int[] expected = [0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x11, 0x12];
            for (int channel = 0; channel < expected.Length; channel++)
            {
                Assert.Equal(expected[channel], operators[channel].Modulator);
                Assert.Equal(expected[channel] + 3, operators[channel].Carrier);
            }
        }

        [Fact]
        public void FrequencyNumbersRiseBySemitoneAcrossOneOctave()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            int[] numbers = parser.ReadFrequencyNumbers();
            _output.WriteLine(string.Join(", ", numbers));

            Assert.Equal(12, numbers.Length);
            for (int semitone = 1; semitone < numbers.Length; semitone++)
            {
                Assert.True(numbers[semitone] > numbers[semitone - 1],
                    $"F-number {semitone} ({numbers[semitone]}) is not above its predecessor.");
            }

            // One octave up is twice the F-number, so the last step must stay below that.
            Assert.True(numbers[11] < numbers[0] * 2);
        }

        [Fact]
        public void InstrumentBankIsReadable()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            List<AdlibInstrument> instruments = parser.ReadInstruments();
            _output.WriteLine($"{instruments.Count} instruments, {instruments.Count(i => i.IsNoise)} of them noise");

            Assert.True(instruments.Count > 32, $"Only {instruments.Count} instruments found.");

            AdlibInstrument first = instruments[0];
            _output.WriteLine($"#0 mod: ar={first.Modulator.AttackRate} dr={first.Modulator.DecayRate} "
                              + $"level={first.Modulator.Level} mult={first.Modulator.FrequencyMultiplier} "
                              + $"fb={first.Modulator.Feedback} fm={first.Modulator.FrequencyModulation}");
            _output.WriteLine($"#0 car: ar={first.Carrier.AttackRate} dr={first.Carrier.DecayRate} "
                              + $"level={first.Carrier.Level} mult={first.Carrier.FrequencyMultiplier}");

            // The carrier of a melodic patch is what you hear, so it must not be silent.
            Assert.True(instruments.Count(i => i.Carrier.Level > 0) > instruments.Count / 2);
        }

        [Fact]
        public void EveryKnownMusicTuneDecodesIntoVoices()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            int decoded = 0;

            for (int tuneId = 0; tuneId <= parser.Layout.MaxTuneId; tuneId++)
            {
                AsoundTuneInfo info = parser.ParseTune(tuneId);
                if (info.Arrangements.Count == 0)
                {
                    _output.WriteLine($"tune {tuneId,2}: {info.Kind} - {info.Diagnostic}");
                    continue;
                }

                decoded++;
                List<AsoundVoiceRef> first = info.Arrangements[0];
                int events = first.Sum(voice => parser.DecodeVoice(voice.DataOffset).Count);

                _output.WriteLine($"tune {tuneId,2}: {info.Kind} arrangements={info.Arrangements.Count} "
                                  + $"voices={first.Count} events={events}"
                                  + (info.Diagnostic == null ? "" : $" [{info.Diagnostic}]"));

                Assert.True(events > 0, $"Tune {tuneId} has voices but no events.");
            }

            Assert.True(decoded > 30, $"Only {decoded} tunes decoded.");
        }

        [Fact]
        public void LeaderThemesOfferFourArrangements()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            for (int tuneId = 5; tuneId <= 18; tuneId++)
            {
                AsoundTuneInfo info = parser.ParseTune(tuneId);
                Assert.Equal(4, info.Arrangements.Count);
                Assert.Equal(TuneScoreKind.Music, info.Kind);
            }
        }

        [Fact]
        public void TitleMusicSpreadsEightVoicesOverConsecutiveChannels()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            AsoundTuneInfo info = parser.ParseTune(3);
            Assert.Single(info.Arrangements);
            Assert.Null(info.Diagnostic);

            List<AsoundVoiceRef> voices = info.Arrangements[0];
            Assert.Equal(8, voices.Count);
            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7], voices.Select(v => v.Channel));

            List<AdlibEvent> lead = parser.DecodeVoice(voices[0].DataOffset);
            _output.WriteLine(string.Join("\n", lead.Take(12).Select(Describe)));

            Assert.Equal(AdlibEventKind.SetInstrument, lead[0].Kind);
            Assert.Contains(lead, e => e.Kind == AdlibEventKind.Note && e.Note > 0);
            Assert.Contains(lead, e => e.Kind == AdlibEventKind.LoopOuter);

            // The title music has no Restart opcode - CIVPLAY replays it, the driver does not loop.
            Assert.DoesNotContain(lead, e => e.Kind == AdlibEventKind.Restart);

            // Every voice runs to the terminator instead of falling off the end of the file.
            foreach (AsoundVoiceRef voice in voices)
            {
                List<AdlibEvent> events = parser.DecodeVoice(voice.DataOffset);
                AdlibEvent last = events[^1];
                Assert.Equal(AdlibEventKind.Note, last.Kind);
                Assert.Equal(0, last.Duration);
            }
        }

        [Fact]
        public void EveryVoiceStreamEndsOnATerminator()
        {
            AsoundParser? parser = TryCreateParser();
            if (parser == null) return;

            var used = new HashSet<AdlibEventKind>();
            int voices = 0;
            int endedByDuration = 0;
            int endedByRestart = 0;

            foreach (List<AdlibEvent> events in AllVoices(parser))
            {
                voices++;
                foreach (AdlibEvent decoded in events) used.Add(decoded.Kind);

                AdlibEvent last = events[^1];
                if (last.Kind == AdlibEventKind.Restart) endedByRestart++;
                else if (last.Kind == AdlibEventKind.Note && last.Duration == 0) endedByDuration++;
                else Assert.Fail($"A voice ends on {last.Kind} at 0x{last.SourceOffset:X4}.");
            }

            _output.WriteLine($"{voices} voices, {endedByDuration} end on a zero duration, "
                              + $"{endedByRestart} on Restart");
            _output.WriteLine(string.Join(", ", used.OrderBy(k => k.ToString(), StringComparer.Ordinal)));

            Assert.True(voices > 300, $"Only {voices} voices found.");
            Assert.Equal(voices, endedByDuration + endedByRestart);

            // Every opcode except the random variant is actually used by the game's music.
            foreach (AdlibEventKind kind in Enum.GetValues<AdlibEventKind>())
            {
                if (kind == AdlibEventKind.RandomVariant) continue;
                Assert.Contains(kind, used);
            }

            Assert.DoesNotContain(AdlibEventKind.RandomVariant, used);
        }

        private static IEnumerable<List<AdlibEvent>> AllVoices(AsoundParser parser)
        {
            for (int tuneId = 0; tuneId <= parser.Layout.MaxTuneId; tuneId++)
            {
                foreach (List<AsoundVoiceRef> arrangement in parser.ParseTune(tuneId).Arrangements)
                {
                    foreach (AsoundVoiceRef voice in arrangement)
                    {
                        yield return parser.DecodeVoice(voice.DataOffset);
                    }
                }
            }
        }

        [Fact]
        public void ConversionWritesABankAndOneFilePerTune()
        {
            string? source = CvlTestFiles.TryFindAsound();
            if (source == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ASOUND.CVL", CvlTestFiles.AsoundEnvironmentVariable));
                return;
            }

            string target = Path.Combine(Path.GetTempPath(), $"asound-convert-{Guid.NewGuid():N}");

            try
            {
                CvlConversionResult result = new CvlSoundConversionService()
                    .ConvertFile(source, target);

                _output.WriteLine(result.Message);
                Assert.True(result.Converted, result.Message);
                Assert.Equal(AsoundCvlConverter.Id, result.PackId);

                string packFolder = Path.Combine(target, AsoundCvlConverter.Id);
                SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));

                Assert.Equal([AdlibSoundBank.FileName], index.SharedFiles);
                Assert.True(index.Tunes.Count > 30, $"Only {index.Tunes.Count} tunes in the index.");

                AdlibSoundBank bank = AdlibScoreJson.LoadBank(Path.Combine(packFolder, AdlibSoundBank.FileName));
                Assert.Equal(9, bank.ChannelCount);
                Assert.Equal(60d, index.FastTickHz / (double)index.WorkerTickDivider);
                Assert.True(bank.Instruments.Count > 32);

                foreach (SoundPackIndexEntry entry in index.Tunes)
                {
                    Assert.NotNull(entry.File);
                    AdlibTuneScore tune = AdlibScoreJson.LoadTune(Path.Combine(packFolder, entry.File!));

                    Assert.Equal(entry.TuneId, tune.TuneId);
                    Assert.Equal(entry.ArrangementCount, tune.Arrangements.Count);

                    foreach (AdlibVoice voice in tune.Arrangements[0].Voices)
                    {
                        Assert.InRange(voice.Channel, 0, bank.ChannelCount - 1);
                    }
                }

                SoundPackIndexEntry leader = index.Tunes.Single(t => t.TuneId == 5);
                Assert.Equal(4, leader.ArrangementCount);

                long bytes = Directory.GetFiles(packFolder).Sum(f => new FileInfo(f).Length);
                _output.WriteLine($"{index.Tunes.Count} tunes, {bank.Instruments.Count} instruments, "
                                  + $"{bytes / 1024} KiB on disk");
            }
            finally
            {
                if (Directory.Exists(target)) Directory.Delete(target, true);
            }
        }

        private static string Describe(AdlibEvent decoded)
            => decoded.Kind == AdlibEventKind.Note
                ? $"  0x{decoded.SourceOffset:X4} note {decoded.Note} for {decoded.Duration}"
                : $"  0x{decoded.SourceOffset:X4} {decoded.Kind} {decoded.Value},{decoded.Delta}";
    }
}
