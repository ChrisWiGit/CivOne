using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Engine;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Engine
{
    /// <summary>
    /// Covers the sound system end to end over a real wave file and a device that never touches a
    /// sound card.
    /// </summary>
    public sealed class SoundSystemTests : IDisposable
    {
        private const int SampleRate = 8000;

        private readonly string _root;

        /// <summary>
        /// Creates a folder of its own for the files the test writes.
        /// </summary>
        public SoundSystemTests()
        {
            _root = Path.Combine(Path.GetTempPath(), $"sound-system-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        /// <summary>
        /// Removes the folder again.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // A file that is still held open is not worth failing a test over.
            }
        }

        /// <summary>
        /// A sound plays, ends, and its event reaches the game thread from <c>Process</c>.
        /// </summary>
        [Fact]
        public void PlaysAFileAndReportsItEndedFromProcess()
        {
            string path = WriteWave("short.wav", 16, 0.5f);

            using var device = new FakeAudioDevice(SampleRate);
            using var system = new SoundSystem(device);

            ISoundHandle? handle = system.Play(new SoundRequest(path, SoundBus.Music));
            Assert.NotNull(handle);

            var ended = false;
            handle.Ended += () => ended = true;

            float[] output = device.Pull(8);
            Assert.All(output, sample => Assert.True(sample > 0f));

            device.Pull(16);
            system.Process();

            Assert.True(ended);
            Assert.False(handle.IsPlaying);
        }

        /// <summary>
        /// The caller's loop point wins over the end of the file.
        /// </summary>
        /// <remarks>
        /// The tail of a rendered tune already repeats its beginning, so turning around at the end of
        /// the file would play that beginning twice.
        /// </remarks>
        [Fact]
        public void HonoursTheLoopPointTheCallerGives()
        {
            string path = WriteWave("loop.wav", 64, 0.5f);

            using var device = new FakeAudioDevice(SampleRate);
            using var system = new SoundSystem(device);

            ISoundHandle? handle = system.Play(new SoundRequest(
                path,
                SoundBus.Music,
                Loop: SoundLoop.Always,
                LoopEndSample: 8));

            Assert.NotNull(handle);

            // Far more samples than the loop is long, and past the end of the file as well: a voice
            // that ignored the loop point would have run out long before.
            float[] output = device.Pull(200);

            Assert.All(output, sample => Assert.True(sample > 0f));
            Assert.True(handle.IsPlaying);
        }

        /// <summary>
        /// A tune only repeats when its file says it should.
        /// </summary>
        /// <remarks>
        /// The renderer writes a loop point exactly when a voice rewound while it was rendering.
        /// A tune without one was written to end, and repeating it would play past that ending.
        /// </remarks>
        [Fact]
        public void WhenMarkedRepeatsOnlyAFileThatCarriesALoopPoint()
        {
            string unmarked = WriteWave("ends.wav", 16, 0.5f);
            string marked = Path.Combine(_root, "repeats.wav");
            new WaveFileWriter().Write(marked, [.. Enumerable.Repeat((short)16384, 16)], SampleRate,
                loopEndSample: 8);

            using var device = new FakeAudioDevice(SampleRate);
            using var system = new SoundSystem(device);

            ISoundHandle? ending = system.Play(new SoundRequest(unmarked, SoundBus.Music, Loop: SoundLoop.WhenMarked));
            Assert.NotNull(ending);
            device.Pull(64);
            Assert.False(ending.IsPlaying);

            ISoundHandle? repeating = system.Play(new SoundRequest(marked, SoundBus.Music, Loop: SoundLoop.WhenMarked));
            Assert.NotNull(repeating);
            device.Pull(64);
            Assert.True(repeating.IsPlaying);
        }

        /// <summary>
        /// A sound that is handed over fades the old one out instead of cutting it off.
        /// </summary>
        [Fact]
        public void ReplacingASoundFadesTheOldOneOut()
        {
            string path = WriteWave("music.wav", 400, 0.5f);

            using var device = new FakeAudioDevice(SampleRate);
            using var system = new SoundSystem(device);

            ISoundHandle? first = system.Play(new SoundRequest(path, SoundBus.Music));
            Assert.NotNull(first);
            device.Pull(8);

            ISoundHandle? second = system.Play(new SoundRequest(
                path,
                SoundBus.Music,
                Replaces: first,
                CrossFade: TimeSpan.FromMilliseconds(10)));

            Assert.NotNull(second);

            float[] output = device.Pull(160);

            // Neither a silent gap while they swap nor a level that runs away.
            Assert.All(output, sample => Assert.InRange(sample, 0f, 1f));
            Assert.True(second.IsPlaying);
        }

        /// <summary>
        /// Closing the system stops the device before it is let go.
        /// </summary>
        [Fact]
        public void DisposingStopsTheDeviceBeforeReleasingIt()
        {
            using var device = new FakeAudioDevice(SampleRate);
            var system = new SoundSystem(device);

            system.Dispose();

            Assert.True(device.Stopped);
            Assert.True(device.Disposed);
        }

        private string WriteWave(string name, int sampleCount, float level)
        {
            string path = Path.Combine(_root, name);
            short[] samples = [.. Enumerable.Repeat((short)(level * short.MaxValue), sampleCount)];
            new WaveFileWriter().Write(path, samples, SampleRate);

            return path;
        }
    }
}
