using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using CivOne.Sound.Playback;
using CivOne.UnitTests.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Renders real tunes end to end: CVL in, wave file out. Skips itself when the original
    /// ASOUND.CVL is not available locally.
    /// </summary>
    public sealed class AdlibRenderTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _root;

        public AdlibRenderTests(ITestOutputHelper output)
        {
            _output = output;
            _root = Path.Combine(Path.GetTempPath(), $"adlib-render-{Guid.NewGuid():N}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        /// <summary>
        /// Converts the real driver into a pack and returns the pack folder, or <c>null</c> when
        /// the driver is not available.
        /// </summary>
        private string? ConvertPack()
        {
            string? source = CvlTestFiles.TryFindAsound();
            if (source == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ASOUND.CVL", CvlTestFiles.AsoundEnvironmentVariable));
                return null;
            }

            CvlConversionResult result = new CvlSoundConversionService().ConvertFile(source, _root);
            Assert.True(result.Converted, result.Message);

            return Path.Combine(_root, AsoundCvlConverter.Id);
        }

        private static SoundPackIndexEntry Tune(string packFolder, int tuneId)
        {
            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));
            return index.Tunes.Single(t => t.TuneId == tuneId);
        }

        /// <summary>
        /// Reads a mono 16-bit wave file back into samples.
        /// </summary>
        private static (short[] Samples, int SampleRate) ReadWave(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));

            int sampleRate = BitConverter.ToInt32(bytes, 24);
            int dataSize = BitConverter.ToInt32(bytes, 40);
            var samples = new short[dataSize / 2];

            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = BitConverter.ToInt16(bytes, 44 + (index * 2));
            }

            return (samples, sampleRate);
        }

        private static (double Peak, double Rms) Levels(short[] samples)
        {
            double peak = 0d;
            double sum = 0d;

            foreach (short sample in samples)
            {
                double value = sample / (double)short.MaxValue;
                peak = Math.Max(peak, Math.Abs(value));
                sum += value * value;
            }

            return (peak, Math.Sqrt(sum / Math.Max(1, samples.Length)));
        }

        [Fact]
        public void TitleMusicRendersToAudibleAudio()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndexEntry entry = Tune(packFolder, 3);
            string? wave = new SoundPackWaveRenderService().Render(packFolder, entry.File!);

            Assert.NotNull(wave);
            (short[] samples, int rate) = ReadWave(wave!);
            (double peak, double rms) = Levels(samples);

            double seconds = samples.Length / (double)rate;
            _output.WriteLine($"{entry.Title}: {seconds:F1} s at {rate} Hz, peak {peak:F3}, rms {rms:F3}");

            Assert.Equal(44100, rate);
            Assert.InRange(seconds, 5d, 240d);
            Assert.InRange(peak, 0.2d, 1.0d);
            Assert.InRange(rms, 0.02d, 0.5d);
        }

        [Fact]
        public void RenderingIsCachedAndReusedUntilTheScoreChanges()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndexEntry entry = Tune(packFolder, 34);
            var service = new SoundPackWaveRenderService();

            string? first = service.Render(packFolder, entry.File!);
            Assert.NotNull(first);
            DateTime written = File.GetLastWriteTimeUtc(first!);

            string? second = service.Render(packFolder, entry.File!);
            Assert.Equal(first, second);
            Assert.Equal(written, File.GetLastWriteTimeUtc(second!));

            // Touching the score invalidates the cache.
            File.SetLastWriteTimeUtc(Path.Combine(packFolder, entry.File!), DateTime.UtcNow.AddMinutes(1));
            string? third = service.Render(packFolder, entry.File!);

            Assert.Equal(first, third);
            Assert.True(File.GetLastWriteTimeUtc(third!) > written, "The cache was not rebuilt.");
        }

        [Fact]
        public void EachArrangementOfALeaderThemeRendersDifferently()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndexEntry entry = Tune(packFolder, 5);
            Assert.Equal(4, entry.ArrangementCount);

            var service = new SoundPackWaveRenderService();
            var lengths = new int[entry.ArrangementCount];

            for (int arrangement = 0; arrangement < entry.ArrangementCount; arrangement++)
            {
                string? wave = service.Render(packFolder, entry.File!, arrangement);
                Assert.NotNull(wave);

                (short[] samples, _) = ReadWave(wave!);
                lengths[arrangement] = samples.Length;

                (double peak, double rms) = Levels(samples);
                _output.WriteLine($"arrangement {arrangement}: {samples.Length} samples, peak {peak:F3}, rms {rms:F3}");

                Assert.True(peak > 0.05d, $"Arrangement {arrangement} is silent.");
            }

            Assert.Equal(entry.ArrangementCount,
                Directory.GetFiles(Path.Combine(packFolder, SoundPackWaveRenderService.CacheFolderName),
                    "05-*.wav").Length);
        }

        [Fact]
        public void EveryTuneOfThePackRenders()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));
            var service = new SoundPackWaveRenderService();

            int rendered = 0;
            double longest = 0d;

            foreach (SoundPackIndexEntry entry in index.Tunes)
            {
                string? wave = service.Render(packFolder, entry.File!);
                Assert.True(wave != null, $"Tune {entry.TuneId} ({entry.Title}) produced no audio.");

                (short[] samples, int rate) = ReadWave(wave!);
                (double peak, _) = Levels(samples);
                double seconds = samples.Length / (double)rate;
                longest = Math.Max(longest, seconds);

                Assert.True(peak > 0.001d, $"Tune {entry.TuneId} ({entry.Title}) is silent.");
                rendered++;
            }

            _output.WriteLine($"{rendered} tunes rendered, longest {longest:F1} s");
            Assert.True(rendered > 30, $"Only {rendered} tunes rendered.");
        }
    }
}
