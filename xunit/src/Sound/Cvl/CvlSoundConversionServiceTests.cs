using System;
using System.IO;
using System.Linq;
using CivOne.Sound;
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
            Assert.Contains($"{SoundNames.MusicTitle}.sound.json", files);
            Assert.Contains($"{SoundNames.LeaderLincoln}.sound.json", files);
            Assert.Contains($"{SoundNames.LeaderMontezuma}.sound.json", files);

            // The evolution music is silent on this driver and gets no file.
            Assert.DoesNotContain($"{SoundNames.MusicEvolution}.sound.json", files);
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
            var tune = TuneScoreJson.Load(Path.Combine(packFolder, $"{SoundNames.MusicTitle}.sound.json"));
            Assert.Equal(3, tune.TuneId);
            Assert.Equal("Title Music", tune.Title);
            Assert.Equal(3, tune.Steps.Count);
        }

        [Fact]
        public void ConvertFolderIndexNamesItsTunesAndListsWhatIsMissing()
        {
            PlaceFakeIsound();
            new CvlSoundConversionService().ConvertFolder(_sourceFolder, _targetFolder);

            var index = SoundPackIndexJson.Load(Path.Combine(_targetFolder, "pc-speaker", SoundPackIndex.FileName));

            Assert.Equal("pc-speaker", index.PackId);
            Assert.Equal("PC Speaker", index.DisplayName);
            Assert.Equal("ISOUND.CVL", index.SourceFile);

            // The fake module has tunes 3, 4, 5, and 6.
            Assert.True(index.TryGetByName(SoundNames.MusicTitle, out SoundPackIndexEntry? title));
            Assert.Equal($"{SoundNames.MusicTitle}.sound.json", title!.File);
            Assert.True(index.TryGetByName(SoundNames.LeaderLincoln, out _));
            Assert.True(index.TryGetByName(SoundNames.LeaderMontezuma, out _));

            // The lookup is case-insensitive, so a differently spelled call site still finds a tune.
            Assert.True(index.TryGetByName(SoundNames.MusicTitle.ToUpperInvariant(), out _));

            // Names the catalog knows but this module has no data for are reported rather than
            // silently disappearing.
            Assert.Contains(SoundNames.CombatWinWeak, index.UnavailableSoundNames);
            Assert.Contains(SoundNames.UiBeep, index.UnavailableSoundNames);
            Assert.DoesNotContain(SoundNames.MusicTitle, index.UnavailableSoundNames);

            // The silent tune appears in the index, but without a file.
            var silent = index.Tunes.Single(t => t.Name == SoundNames.MusicEvolution);
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

            // Music, all 14 long and short leader themes, the city view flourish, the beep and
            // the four combat outcomes should all be there.
            Assert.True(index.TryGetByName(SoundNames.MusicWin, out _));
            Assert.True(index.TryGetByName(SoundNames.MusicLose, out _));
            Assert.True(index.TryGetByName(SoundNames.LeaderAlexander, out _));

            // The audience sting and the alarm have no data on the PC speaker driver.
            Assert.Equal(
                [SoundNames.EventAudience, SoundNames.EventAlarm],
                index.UnavailableSoundNames);

            string winFile = index.Tunes.Single(t => t.Name == SoundNames.MusicWin).File
                ?? throw new InvalidOperationException("Win Music should have a file.");
            var win = TuneScoreJson.Load(Path.Combine(packFolder, winFile));
            Assert.Equal(3320, win.Steps[0].Divisor);
        }
    }
}
