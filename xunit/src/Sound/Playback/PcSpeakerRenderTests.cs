using System;
using System.IO;
using System.Linq;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;
using CivOne.UnitTests.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers the PC speaker renderer end to end over the synthetic ISOUND module, so it runs
    /// without any original game data.
    /// </summary>
    public sealed class PcSpeakerRenderTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _root;

        public PcSpeakerRenderTests(ITestOutputHelper output)
        {
            _output = output;
            _root = Path.Combine(Path.GetTempPath(), $"pcs-render-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_root, "source"));

            File.WriteAllBytes(Path.Combine(_root, "source", "ISOUND.CVL"), FakeIsoundModule.Build());
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private string ConvertPack()
        {
            string target = Path.Combine(_root, "sounds");
            CvlConversionReport report = new CvlSoundConversionService()
                .ConvertFolder(Path.Combine(_root, "source"), target);

            foreach (string message in report.Messages) _output.WriteLine(message);
            Assert.True(report.AnyConverted);

            return Path.Combine(target, IsoundCvlConverter.Id);
        }

        private static (short[] Samples, int SampleRate) ReadWave(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int sampleRate = BitConverter.ToInt32(bytes, 24);
            int dataSize = BitConverter.ToInt32(bytes, 40);

            var samples = new short[dataSize / 2];
            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = BitConverter.ToInt16(bytes, 44 + (index * 2));
            }

            return (samples, sampleRate);
        }

        [Fact]
        public void EveryTuneOfThePackRendersToAudibleAudio()
        {
            string packFolder = ConvertPack();
            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));
            var service = new SoundPackWaveRenderService();

            int rendered = 0;

            foreach (SoundPackIndexEntry entry in index.Tunes.Where(t => t.File != null))
            {
                string? wave = service.Render(packFolder, entry.File!);
                Assert.True(wave != null, $"Tune {entry.Name} ({entry.Title}) produced no audio.");

                (short[] samples, int rate) = ReadWave(wave!);
                double peak = samples.Length == 0 ? 0d : samples.Max(s => Math.Abs(s / (double)short.MaxValue));

                _output.WriteLine($"{entry.Name,-26} {entry.Title,-16} "
                                  + $"{samples.Length / (double)rate:F2} s, peak {peak:F3}");

                Assert.Equal(44100, rate);
                Assert.True(peak > 0.05d, $"Tune {entry.Name} is silent.");
                rendered++;
            }

            Assert.True(rendered > 0);
        }

        [Fact]
        public void SilentTunesGetNoFileAndNoAudio()
        {
            string packFolder = ConvertPack();
            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));

            SoundPackIndexEntry silent = index.Tunes.Single(t => t.Name == CvlTuneCatalog.ResolveName(FakeIsoundModule.TuneSilent));
            Assert.Null(silent.File);
            Assert.Equal(TuneScoreKind.Silent, silent.Kind);
        }

        [Fact]
        public void APackWithoutAnIndexIsNotRendered()
        {
            string packFolder = ConvertPack();
            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));
            string fileName = index.Tunes.First(t => t.File != null).File!;

            File.Delete(Path.Combine(packFolder, SoundPackIndex.FileName));

            Assert.Null(new SoundPackWaveRenderService().Render(packFolder, fileName));
        }

        [Fact]
        public void ARestIsSilentWhileAToneIsNot()
        {
            var tune = new TuneScore
            {
                TuneId = 3,
                Title = "Test",
                Kind = TuneScoreKind.Music,
                Steps =
                [
                    new TuneStep { Duration = 6, Divisor = 0 },
                    new TuneStep { Duration = 6, Divisor = 2712, NoiseMask = 1 }
                ]
            };

            var index = new SoundPackIndex
            {
                PackId = "test",
                DisplayName = "Test",
                Driver = "ISOUND",
                Device = PcSpeakerTuneRenderer.DeviceName,
                PitClockHz = 1_193_182
            };

            string folder = Path.Combine(_root, "manual");
            Directory.CreateDirectory(folder);
            TuneScoreJson.Save(Path.Combine(folder, "03-test.sound.json"), tune);

            RenderedTune? rendered = new PcSpeakerTuneRenderer().Render(index, folder, "03-test.sound.json", 0);
            Assert.NotNull(rendered);

            float[] samples = rendered!.Value.Samples;
            int half = samples.Length / 2;

            double restPeak = samples.Take(half).Max(Math.Abs);
            double tonePeak = samples.Skip(half).Max(Math.Abs);

            _output.WriteLine($"rest peak {restPeak:F4}, tone peak {tonePeak:F4}");

            Assert.Equal(0d, restPeak);
            Assert.True(tonePeak > 0.5d, $"The tone is too quiet: {tonePeak}");
        }

        /// <summary>
        /// The driver's effect table holds slide words such as <c>0xD204</c> whose value is far
        /// larger than the divisor they are added to. The timer register wraps, which turns the
        /// note into the deep percussion of the original tunes; reading the word as a signed delta
        /// instead drives the divisor below zero and drops the note entirely.
        /// </summary>
        [Theory]
        [InlineData(0xD204)]
        [InlineData(0xCC05)]
        [InlineData(0xDC73)]
        public void ALargeSlideWrapsIntoADeepToneInsteadOfSilence(int effect)
        {
            var tune = new TuneScore
            {
                TuneId = 3,
                Title = "Test",
                Kind = TuneScoreKind.Music,
                Steps =
                [
                    new TuneStep { Duration = 30, Divisor = 7448, NoiseMask = 1, Effect = effect }
                ]
            };

            var index = new SoundPackIndex
            {
                PackId = "test",
                DisplayName = "Test",
                Driver = "ISOUND",
                Device = PcSpeakerTuneRenderer.DeviceName,
                PitClockHz = 1_193_182
            };

            string folder = Path.Combine(_root, $"slide-{effect:X4}");
            Directory.CreateDirectory(folder);
            TuneScoreJson.Save(Path.Combine(folder, "03-test.sound.json"), tune);

            RenderedTune? rendered = new PcSpeakerTuneRenderer().Render(index, folder, "03-test.sound.json", 0);
            Assert.NotNull(rendered);

            float[] samples = rendered!.Value.Samples;

            // The first worker tick still carries the note's own divisor, so only what follows it
            // shows whether the slide survived.
            int afterFirstTick = samples.Length / 10;
            double peak = samples.Skip(afterFirstTick).Max(Math.Abs);

            _output.WriteLine($"effect 0x{effect:X4}: peak after the first tick {peak:F4}");

            Assert.True(peak > 0.5d, $"Effect 0x{effect:X4} silences the note: {peak}");
        }
    }
}
