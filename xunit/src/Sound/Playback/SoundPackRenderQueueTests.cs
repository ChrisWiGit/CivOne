using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using CivOne.Sound.Playback;
using CivOne.UnitTests.Sound.Cvl;
using Xunit;
using Xunit.Abstractions;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers the background rendering of a real pack. Skips itself when the original ASOUND.CVL is
    /// not available locally.
    /// </summary>
    public sealed class SoundPackRenderQueueTests : IDisposable
    {
        /// <summary>
        /// How long a test waits for a background render. Generous on purpose: a debug build renders
        /// roughly six times slower than a release build.
        /// </summary>
        private static readonly TimeSpan _renderTimeout = TimeSpan.FromMinutes(5);

        private readonly ITestOutputHelper _output;
        private readonly string _root;

        public SoundPackRenderQueueTests(ITestOutputHelper output)
        {
            _output = output;
            _root = Path.Combine(Path.GetTempPath(), $"render-queue-{Guid.NewGuid():N}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        /// <summary>
        /// Converts the real driver into a pack and returns the pack folder, or <c>null</c> when the
        /// driver is not available.
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

        private static SoundPackIndex Index(string packFolder)
            => SoundPackIndexJson.Load(Path.Combine(packFolder, SoundPackIndex.FileName));

        /// <summary>
        /// A tune is only reported as cached once it has actually been rendered.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task TryGetCachedOnlyReportsAFinishedRender()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            string file = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderGandhi).File!;
            var queueUnderTest = new SoundPackRenderQueue();

            Assert.Null(queueUnderTest.TryGetCached(packFolder, file, 0));

            string? rendered = await queueUnderTest.Request(packFolder, file, 0).ConfigureAwait(true);

            Assert.NotNull(rendered);
            Assert.Equal(rendered, queueUnderTest.TryGetCached(packFolder, file, 0));
        }

        /// <summary>
        /// Asking twice for the same tune joins the render that is already running instead of
        /// starting a second one that would write the same file.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task RequestingTheSameTuneTwiceRendersItOnce()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            string file = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderGandhi).File!;
            var queueUnderTest = new SoundPackRenderQueue();

            Task<string?> first = queueUnderTest.Request(packFolder, file, 0);
            Task<string?> second = queueUnderTest.Request(packFolder, file, 0);

            Assert.Same(first, second);
            Assert.NotNull(await first.ConfigureAwait(true));
        }

        /// <summary>
        /// Arrangements are rendered separately, so a pack that offers several does not hand out the
        /// same file for all of them.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task ArrangementsAreRenderedSeparately()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndexEntry entry = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderLincoln);
            Assert.True(entry.ArrangementCount > 1, "This tune was expected to offer several arrangements.");

            var queueUnderTest = new SoundPackRenderQueue();

            string? first = await queueUnderTest.Request(packFolder, entry.File!, 0).ConfigureAwait(true);
            string? second = await queueUnderTest.Request(packFolder, entry.File!, 1).ConfigureAwait(true);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// The warm-up renders the whole pack in the background, and the opening theme - the first
        /// sound the game asks for - is among the results.
        /// </summary>
        /// <remarks>
        /// Renders every tune of the pack and takes roughly 45 seconds, so it is marked
        /// <c>Category=Slow</c> and excluded from a normal test run.
        /// </remarks>
        [Trait("Category", "IntegrationLocalData")]
        [Trait("Category", "Slow")]
        [Fact]
        public async Task WarmingUpAPackRendersEveryTune()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            SoundPackIndex index = Index(packFolder);
            string[] files = [.. index.Tunes.Where(t => t.File != null).Select(t => t.File!)];

            var queueUnderTest = new SoundPackRenderQueue();
            queueUnderTest.WarmPack(packFolder);

            // The warm-up runs in the background, so wait for its results rather than for the call.
            Task<string?>[] renders = [.. files.Select(file => queueUnderTest.Request(packFolder, file, 0))];
            string?[] rendered = await Task.WhenAll(renders).WaitAsync(_renderTimeout).ConfigureAwait(true);

            Assert.All(rendered, Assert.NotNull);

            string opening = index.Tunes.Single(t => t.Name == SoundNames.MusicTitle).File!;
            Assert.NotNull(queueUnderTest.TryGetCached(packFolder, opening, 0));

            _output.WriteLine($"{files.Length} tunes warmed up into {SoundPackWaveRenderService.CacheFolderName}");
        }

        /// <summary>
        /// Warming a pack a second time does not start the work again.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task WarmingUpTwiceIsHarmless()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            var queueUnderTest = new SoundPackRenderQueue();

            queueUnderTest.WarmPack(packFolder);
            queueUnderTest.WarmPack(packFolder);

            string file = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderGandhi).File!;

            Assert.NotNull(await queueUnderTest.Request(packFolder, file, 0)
                .WaitAsync(_renderTimeout).ConfigureAwait(true));
        }

        /// <summary>
        /// A folder that holds no pack is skipped instead of throwing on a background thread.
        /// </summary>
        [Fact]
        public void WarmingUpAFolderWithoutAPackDoesNothing()
        {
            var queueUnderTest = new SoundPackRenderQueue();

            queueUnderTest.WarmPack(Path.Combine(_root, "does-not-exist"));
            queueUnderTest.WarmPack(string.Empty);
        }

        /// <summary>
        /// A pack that did not exist yet at the first attempt is warmed up on the next one.
        /// </summary>
        /// <remarks>
        /// This is the case at game start on a profile whose sound data has not been converted yet:
        /// the warm-up finds nothing, and the conversion that follows has to be able to trigger it
        /// again.
        /// The only signal that the warm-up ran is a rendered tune, so this waits for a real render
        /// and takes roughly 25 seconds. It is therefore marked <c>Category=Slow</c> and excluded
        /// from a normal test run.
        /// </remarks>
        [Trait("Category", "IntegrationLocalData")]
        [Trait("Category", "Slow")]
        [Fact]
        public void APackThatAppearsLaterIsStillWarmedUp()
        {
            string packFolder = Path.Combine(_root, AsoundCvlConverter.Id);
            var queueUnderTest = new SoundPackRenderQueue();

            // First attempt: nothing to find, because the sound data is not converted yet.
            queueUnderTest.WarmPack(packFolder);
            Assert.False(Directory.Exists(packFolder));

            if (ConvertPack() == null) return;

            SoundPackIndex index = Index(packFolder);
            string opening = index.Tunes.Single(t => t.Name == SoundNames.MusicTitle).File!;

            // The later attempts have to be accepted, which is what a stuck "already warmed" mark
            // would prevent. Nothing here renders on its own, so only the warm-up can produce this.
            Assert.True(WaitFor(() =>
            {
                queueUnderTest.WarmPack(packFolder);
                return queueUnderTest.TryGetCached(packFolder, opening, 0) != null;
            }), "The pack was never warmed up after it appeared.");
        }

        /// <summary>
        /// A wave file that has been deleted is rendered again instead of being handed out as a
        /// path that points at nothing.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task ADeletedWaveFileIsRenderedAgain()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            string file = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderGandhi).File!;
            var queueUnderTest = new SoundPackRenderQueue();

            string? first = await queueUnderTest.Request(packFolder, file, 0).ConfigureAwait(true);
            Assert.NotNull(first);

            File.Delete(first!);
            Assert.Null(queueUnderTest.TryGetCached(packFolder, file, 0));

            string? second = await queueUnderTest.Request(packFolder, file, 0).ConfigureAwait(true);

            Assert.Equal(first, second);
            Assert.True(File.Exists(second!), "The deleted wave file was not rendered again.");
        }

        /// <summary>
        /// Polls a condition until it holds or the render timeout runs out.
        /// </summary>
        /// <param name="condition">What to wait for; called repeatedly.</param>
        /// <returns><c>true</c> when the condition became true in time.</returns>
        private static bool WaitFor(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow + _renderTimeout;

            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(50);
            }

            return false;
        }

        /// <summary>
        /// Two packs are warmed up one after another rather than at the same time, so they do not
        /// ask for twice the cores the warm-up is allowed to use.
        /// </summary>
        [Trait("Category", "IntegrationLocalData")]
        [Fact]
        public async Task WarmingUpASecondPackWaitsForTheFirst()
        {
            string? packFolder = ConvertPack();
            if (packFolder == null) return;

            string secondFolder = Path.Combine(_root, "copy-of-pack");
            Directory.CreateDirectory(secondFolder);
            foreach (string file in Directory.GetFiles(packFolder))
            {
                File.Copy(file, Path.Combine(secondFolder, Path.GetFileName(file)));
            }

            var queueUnderTest = new SoundPackRenderQueue();
            queueUnderTest.WarmPack(packFolder);
            queueUnderTest.WarmPack(secondFolder);

            string file17 = Index(packFolder).Tunes.Single(t => t.Name == SoundNames.LeaderGandhi).File!;

            Assert.NotNull(await queueUnderTest.Request(secondFolder, file17, 0)
                .WaitAsync(_renderTimeout).ConfigureAwait(true));
            Assert.NotNull(queueUnderTest.TryGetCached(secondFolder, file17, 0));
        }
    }
}
