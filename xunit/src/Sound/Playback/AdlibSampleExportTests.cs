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
    /// Writes rendered tunes somewhere they can be listened to, for checking the result by ear
    /// against DOSBox.
    /// </summary>
    /// <remarks>
    /// Off by default: set <c>CIVONE_ADLIB_SAMPLE_DIR</c> to a folder to have the tunes written
    /// there. Nothing is exported otherwise, so a normal test run stays quiet.
    /// Marked <c>Category=Slow</c> because it renders the whole pack once it is switched on.
    /// </remarks>
    [Trait("Category", "IntegrationLocalData")]
    [Trait("Category", "Slow")]
    public sealed class AdlibSampleExportTests
    {
        /// <summary>Environment variable that names the folder to export to.</summary>
        public const string TargetVariable = "CIVONE_ADLIB_SAMPLE_DIR";

        private readonly ITestOutputHelper _output;

        public AdlibSampleExportTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void ExportTunesForListening()
        {
            string? target = Environment.GetEnvironmentVariable(TargetVariable);
            if (string.IsNullOrWhiteSpace(target))
            {
                _output.WriteLine($"Skipped: set {TargetVariable} to a folder to export the tunes.");
                return;
            }

            string? source = CvlTestFiles.TryFindAsound();
            if (source == null)
            {
                _output.WriteLine(CvlTestFiles.MissingHint("ASOUND.CVL", CvlTestFiles.AsoundEnvironmentVariable));
                return;
            }

            Directory.CreateDirectory(target);

            CvlConversionResult result = new CvlSoundConversionService().ConvertFile(source, target);
            Assert.True(result.Converted, result.Message);
            _output.WriteLine(result.Message);

            string packFolder = Path.Combine(target, AsoundCvlConverter.Id);
            SoundPackIndex index = SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));
            var service = new SoundPackWaveRenderService();

            foreach (SoundPackIndexEntry entry in index.Tunes.OrderBy(t => t.TuneId))
            {
                string? wave = service.Render(packFolder, entry.File!);
                if (wave == null) continue;

                var info = new FileInfo(wave);
                _output.WriteLine($"{entry.TuneId,2} {entry.Title,-20} {info.Length / 1024,6} KiB  {info.Name}");
            }

            _output.WriteLine($"Exported to {Path.Combine(packFolder, SoundPackWaveRenderService.CacheFolderName)}");
        }
    }
}
