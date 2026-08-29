using System.Collections.Generic;
using System.Threading.Tasks;
using CivOne;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
    /// <summary>
    /// Covers that a sound pack tune never makes the game thread wait for a render, and that the
    /// sound still starts once the render is done.
    /// </summary>
    public sealed class SoundPackPlaybackServiceTests
    {
        private const string PackId = "test-pack";

        private readonly MockRuntime _runtime;
        private readonly FakeRenderQueue _queue = new();
        private readonly SoundPackPlaybackService _serviceUnderTest;

        /// <summary>
        /// Registers a runtime, which the service plays through, and wires it to a queue that
        /// renders nothing so the test controls when a render finishes.
        /// </summary>
        public SoundPackPlaybackServiceTests()
        {
            _runtime = new MockRuntime(new RuntimeSettings());
            _serviceUnderTest = new SoundPackPlaybackService(_runtime, _queue, new ArrangementPickerDelegate(seed: 1));
        }

        private static SoundPackIndexEntry Tune(string file = "03-title.sound.json")
            => new() { TuneId = 3, Title = "Title Music", File = file };

        /// <summary>
        /// A tune that is already rendered is handed to the runtime right away.
        /// </summary>
        [Fact]
        public void ACachedTuneStartsImmediately()
        {
            _queue.Cached = "title.wav";

            Assert.True(_serviceUnderTest.TryPlayTune(PackId, Tune(), 0));

            Assert.Equal(["title.wav"], _runtime.PlayedSounds);
            Assert.Empty(_queue.Requested);
        }

        /// <summary>
        /// A tune that still has to be rendered is queued, and nothing is played yet - this is what
        /// used to freeze the game for seconds.
        /// </summary>
        [Fact]
        public void AnUncachedTuneIsQueuedWithoutPlayingAnything()
        {
            Assert.True(_serviceUnderTest.TryPlayTune(PackId, Tune(), 0));

            Assert.Empty(_runtime.PlayedSounds);
            Assert.Equal(["03-title.sound.json"], _queue.Requested);
        }

        /// <summary>
        /// While the render is still running, nothing happens on the game thread.
        /// </summary>
        [Fact]
        public void ProcessDoesNothingWhileTheRenderIsRunning()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);
            _serviceUnderTest.Process();

            Assert.Empty(_runtime.PlayedSounds);
        }

        /// <summary>
        /// Once the render is done, the next frame starts the sound.
        /// </summary>
        [Fact]
        public void TheTuneStartsOnceItsRenderIsDone()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);
            _queue.Complete(0, "title.wav");

            _serviceUnderTest.Process();

            Assert.Equal(["title.wav"], _runtime.PlayedSounds);
        }

        /// <summary>
        /// A second frame does not start the same sound again.
        /// </summary>
        [Fact]
        public void AStartedSoundIsNotStartedTwice()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);
            _queue.Complete(0, "title.wav");

            _serviceUnderTest.Process();
            _serviceUnderTest.Process();

            Assert.Equal(["title.wav"], _runtime.PlayedSounds);
        }

        /// <summary>
        /// A tune that cannot be rendered is simply silent.
        /// </summary>
        [Fact]
        public void AFailedRenderStaysSilent()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);
            _queue.Complete(0, null);

            _serviceUnderTest.Process();

            Assert.Empty(_runtime.PlayedSounds);
        }

        /// <summary>
        /// Aborting drops a sound that has not started yet, so it cannot cut in afterwards.
        /// </summary>
        [Fact]
        public void CancelPendingDropsTheWaitingSound()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);
            _serviceUnderTest.CancelPending();

            _queue.Complete(0, "title.wav");
            _serviceUnderTest.Process();

            Assert.Empty(_runtime.PlayedSounds);
        }

        /// <summary>
        /// Asking for another sound replaces the one that is still waiting, the same way a new sound
        /// replaces a playing one.
        /// </summary>
        [Fact]
        public void ANewSoundSupersedesTheWaitingOne()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune("03-title.sound.json"), 0);
            _serviceUnderTest.TryPlayTune(PackId, Tune("09-napoleon.sound.json"), 0);

            _queue.Complete(0, "title.wav");
            _serviceUnderTest.Process();
            Assert.Empty(_runtime.PlayedSounds);

            _queue.Complete(1, "napoleon.wav");
            _serviceUnderTest.Process();
            Assert.Equal(["napoleon.wav"], _runtime.PlayedSounds);
        }

        /// <summary>
        /// The first tune of a pack also starts the background warm-up of the rest.
        /// </summary>
        [Fact]
        public void TheFirstTuneWarmsUpTheWholePack()
        {
            _serviceUnderTest.TryPlayTune(PackId, Tune(), 0);

            Assert.Single(_queue.Warmed);
            Assert.EndsWith(PackId, _queue.Warmed[0], System.StringComparison.Ordinal);
        }

        /// <summary>
        /// A pack can be warmed up before any sound is due, which is what keeps the first tune of a
        /// session from having to wait for its render.
        /// </summary>
        [Fact]
        public void WarmUpStartsAPackWithoutPlayingAnything()
        {
            _serviceUnderTest.WarmUp(PackId);

            Assert.Single(_queue.Warmed);
            Assert.EndsWith(PackId, _queue.Warmed[0], System.StringComparison.Ordinal);
            Assert.Empty(_runtime.PlayedSounds);
            Assert.Empty(_queue.Requested);
        }

        /// <summary>
        /// Without a pack there is nothing to warm up, which is the case for wave files and silence.
        /// </summary>
        [Fact]
        public void WarmUpWithoutAPackDoesNothing()
        {
            _serviceUnderTest.WarmUp(string.Empty);

            Assert.Empty(_queue.Warmed);
        }

        /// <summary>
        /// A queue that renders nothing by itself, so a test decides when a render finishes.
        /// </summary>
        private sealed class FakeRenderQueue : ISoundPackRenderQueue
        {
            private readonly List<TaskCompletionSource<string?>> _requests = [];

            /// <summary>Path <see cref="TryGetCached"/> reports, or <c>null</c> for "not rendered yet".</summary>
            public string? Cached { get; set; }

            /// <summary>Tune files that were queued for rendering, in order.</summary>
            public List<string> Requested { get; } = [];

            /// <summary>Pack folders a warm-up was started for.</summary>
            public List<string> Warmed { get; } = [];

            public string? TryGetCached(string packFolder, string fileName, int arrangement) => Cached;

            public Task<string?> Request(string packFolder, string fileName, int arrangement)
            {
                var request = new TaskCompletionSource<string?>();
                _requests.Add(request);
                Requested.Add(fileName);

                return request.Task;
            }

            public void WarmPack(string packFolder) => Warmed.Add(packFolder);

            /// <summary>
            /// Finishes a queued render.
            /// </summary>
            /// <param name="request">Index of the request, in the order they were made.</param>
            /// <param name="path">The rendered file, or <c>null</c> when the render failed.</param>
            public void Complete(int request, string? path) => _requests[request].TrySetResult(path);
        }
    }
}
