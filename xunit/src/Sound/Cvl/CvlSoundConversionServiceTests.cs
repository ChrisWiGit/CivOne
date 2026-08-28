using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Covers the one-off conversion run: CVL folder in, a pack folder with one file per
    /// tune plus an index.json out.
    /// </summary>
    public sealed class CvlSoundConversionServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _sourceFolder;
        private readonly string _targetFolder;

        public CvlSoundConversionServiceTests(ITestOutputHelper output)
        {
            _output = output;

            string root = Path.Combine(Path.GetTempPath(), $"cvl-convert-{Guid.NewGuid():N}");
            _sourceFolder = Path.Combine(root, "source");
            _targetFolder = Path.Combine(root, "sounds");
            Directory.CreateDirectory(_sourceFolder);
            Directory.CreateDirectory(_targetFolder);
        }

        public void Dispose()
        {
            string? root = Directory.GetParent(_sourceFolder)?.FullName;
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        private void PlaceFakeIsound(string fileName = "ISOUND.CVL")
        {
            string path = Path.Combine(_sourceFolder, fileName);
            File.WriteAllBytes(path, FakeIsoundModule.Build());
        }

        [Fact]
        public void DeviceDetectorRecognisesSpeakerDriver()
            => Assert.Equal("PcSpeaker",
                CvlDeviceDetector.Detect(CvlImage.FromBytes(FakeIsoundModule.Build(), "fake.cvl")).ToString());

        [Fact]
        public void IsoundConverterAcceptsSpeakerModule()
        {
            var converter = new IsoundCvlConverter();
            var image = CvlImage.FromBytes(FakeIsoundModule.Build(), "fake.cvl");

            Assert.True(converter.CanConvert(image, out string? reason), reason);
            Assert.Equal("pc-speaker", converter.PackId);
        }

        [Fact]
        public void ConvertFolderWritesOneFilePerTuneAndAnIndex()
        {
            PlaceFakeIsound();

            var report = new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder);
            foreach (string message in report.Messages) _output.WriteLine(message);

            Assert.True(report.AnyConverted);
            var result = report.Results.Single();
            Assert.Equal("pc-speaker", result.PackId);

            string packFolder = Path.Combine(_targetFolder, "pc-speaker");
            Assert.True(Directory.Exists(packFolder));

            var files = Directory.GetFiles(packFolder, "*.sound.json").Select(Path.GetFileName).OrderBy(x => x).ToArray();
            Assert.Equal(3, files.Length);
            Assert.Contains("03-title-music.sound.json", files);
            Assert.Contains("05-lincoln-long.sound.json", files);
            Assert.Contains("06-montezuma-long.sound.json", files);

            // The silent tune 4 gets no file.
            Assert.DoesNotContain(files, f => f!.StartsWith("04-", StringComparison.Ordinal));
        }

        [Fact]
        public void ConvertFolderWritesSelfContainedTuneFiles()
        {
            PlaceFakeIsound();
            new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder);

            string packFolder = Path.Combine(_targetFolder, "pc-speaker");
            var index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));

            // The pack metadata lives in exactly one place, the manifest.
            Assert.Equal("pc-speaker", index.PackId);
            Assert.Equal("ISOUND", index.Driver);
            Assert.Equal("pcSpeaker", index.Device);
            Assert.Equal(1_193_182, index.PitClockHz);
            Assert.Equal(300, index.FastTickHz);
            Assert.Equal(5, index.WorkerTickDivider);
            Assert.Empty(index.SharedFiles);

            // The tune file now only contains the tune itself.
            var tune = TuneScoreJson.Load(Path.Combine(packFolder, "03-title-music.sound.json"));
            Assert.Equal(3, tune.TuneId);
            Assert.Equal("Title Music", tune.Title);
            Assert.Equal(3, tune.Steps.Count);
        }

        [Fact]
        public void ConvertFolderIndexMapsEngineSoundNamesAndListsTheRest()
        {
            PlaceFakeIsound();
            new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder);

            var index = SoundPackIndexJson.Load(Path.Combine(_targetFolder, "pc-speaker", SoundPackIndex.FileName));

            Assert.Equal("pc-speaker", index.PackId);
            Assert.Equal("PC Speaker", index.DisplayName);
            Assert.Equal("ISOUND.CVL", index.SourceFile);

            // The fake module has tunes 3, 4, 5, and 6.
            Assert.Equal(3, index.SoundNames["opening"]);
            Assert.Equal(5, index.SoundNames["linc"]);
            Assert.Equal(6, index.SoundNames["mont"]);

            // Mapping is case-insensitive; the engine calls "OPENING".
            Assert.Equal(3, index.SoundNames["OPENING"]);

            // Effects are not mapped yet and are reported instead of silently disappearing.
            Assert.Contains("cannon", index.UnmappedSoundNames);
            Assert.Contains("s_beep", index.UnmappedSoundNames);
            Assert.DoesNotContain("opening", index.UnmappedSoundNames);

            // The silent tune appears in the index, but without a file.
            var silent = index.Tunes.Single(t => t.TuneId == 4);
            Assert.Equal("Silent", silent.Kind.ToString());
            Assert.Null(silent.File);
        }

        [Fact]
        public void ConvertFolderSkipsUnsupportedModulesWithAMessage()
        {
            // Valid CVL, but without a recognizable device.
            byte[] bytes = FakeIsoundModule.Build();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0xE6) bytes[i] = 0x90;
            }

            File.WriteAllBytes(Path.Combine(_sourceFolder, "XSOUND.CVL"), bytes);

            var report = new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder);
            var result = report.Results.Single();

            Assert.False(result.Converted);
            Assert.Contains("no converter", result.Message);
            Assert.Empty(Directory.GetDirectories(_targetFolder));
        }

        [Fact]
        public void ConvertFolderReportsUnreadableFiles()
        {
            File.WriteAllBytes(Path.Combine(_sourceFolder, "BROKEN.CVL"), new byte[16]);

            var result = new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder).Results.Single();

            Assert.False(result.Converted);
            Assert.Contains("not readable", result.Message);
        }

        [Fact]
        public void ConvertFolderReportsEmptySourceFolder()
        {
            var result = new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder).Results.Single();

            Assert.False(result.Converted);
            Assert.Contains("No CVL files found", result.Message);
        }

        [Theory]
        [InlineData("Title Music", "title-music")]
        [InlineData("Alexander the Great", "alexander-the-great")]
        [InlineData("Tune 19", "tune-19")]
        [InlineData("  ", "tune")]
        public void SlugMakesFileSystemSafeNames(string title, string expected)
            => Assert.Equal(expected, CvlSoundConversionService.Slug(title));

        // -----------------------------------------------------------------------------
        // Opt-in: the real modules, if available locally.
        // -----------------------------------------------------------------------------

        [Trait("Category", "IntegrationLocalData")]
        [Theory]
        [InlineData("ISOUND.CVL", "PcSpeaker")]
        [InlineData("ASOUND.CVL", "AdLib")]
        [InlineData("TSOUND.CVL", "Tandy")]
        [InlineData("RSOUND.CVL", "Roland")]
        [InlineData("NSOUND.CVL", "Silent")]
        public void RealModulesAreDetectedByPortUsage(string fileName, string expectedDevice)
        {
            string? isound = CvlTestFiles.TryFindIsound();
            if (isound == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint(fileName, CvlTestFiles.IsoundEnvironmentVariable));
                return;
            }

            string path = Path.Combine(Path.GetDirectoryName(isound) ?? string.Empty, fileName);
            if (!File.Exists(path))
            {
                _output.WriteLine($"Skipped: {path} does not exist.");
                return;
            }

            var image = CvlImage.Load(path);
            _output.WriteLine($"{fileName}: signature '{image.Signature}', data segment 0x{image.DataSegment:X4}");

            Assert.Equal(expectedDevice, CvlDeviceDetector.Detect(image).ToString());
        }

        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public void RealIsoundConvertsIntoAUsablePack()
        {
            string? isound = CvlTestFiles.TryFindIsound();
            if (isound == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ISOUND.CVL", CvlTestFiles.IsoundEnvironmentVariable));
                return;
            }

            File.Copy(isound, Path.Combine(_sourceFolder, "ISOUND.CVL"), true);

            // Set CIVONE_SOUND_OUT to persist the result somewhere for inspection.
            string? configured = Environment.GetEnvironmentVariable("CIVONE_SOUND_OUT");
            string targetFolder = string.IsNullOrWhiteSpace(configured) ? _targetFolder : configured;

            var report = new CvlSoundConversionService().ConvertFolder(_sourceFolder, targetFolder);
            foreach (string message in report.Messages) _output.WriteLine(message);

            var result = report.Results.Single(r => r.Converted);
            Assert.Equal("pc-speaker", result.PackId);
            Assert.True(result.TuneCount >= 20, $"Too few tunes: {result.TuneCount}.");

            string packFolder = Path.Combine(targetFolder, "pc-speaker");
            Assert.Equal(result.TuneCount, Directory.GetFiles(packFolder, "*.sound.json").Length);

            var index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));

            // Music, all 14 long and short leader themes, cityview, the three identified
            // effects, and the four combat outcome names (combat_*) should be mapped.
            Assert.Equal(41, index.SoundNames.Count);
            Assert.Equal(34, index.SoundNames["wintune"]);
            Assert.Equal(35, index.SoundNames["lose2"]);
            Assert.Equal(12, index.SoundNames["alex"]);

            // The four combat names actually extracted but no longer invoked remain unmapped
            // (see SoundNameMap); audience and alarm have no data on the PC speaker driver
            // (like tune 4).
            Assert.Equal(6, index.UnmappedSoundNames.Count);

            string winFile = index.Tunes.Single(t => t.TuneId == 34).File
                ?? throw new InvalidOperationException("Win Music should have a file.");
            var win = TuneScoreJson.Load(Path.Combine(packFolder, winFile));
            Assert.Equal(3320, win.Steps[0].Divisor);
        }
    }
}
