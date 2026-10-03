using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using CivOne.Sound.Engine;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Engine
{
    /// <summary>
    /// Covers reading the wave files the renderer writes, including the loop point a looping tune
    /// needs.
    /// </summary>
    public sealed class WaveSampleLoaderTests : IDisposable
    {
        private readonly string _root;
        private readonly WaveSampleLoaderDelegate _loader = new();

        /// <summary>
        /// Creates a folder of its own for the files the test writes.
        /// </summary>
        public WaveSampleLoaderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), $"wave-loader-{Guid.NewGuid():N}");
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
        /// What the renderer writes has to read back as the same samples.
        /// </summary>
        [Fact]
        public void ReadsBackWhatTheWriterWrote()
        {
            short[] written = [0, 16384, -16384, 32767];
            string path = Path.Combine(_root, "tune.wav");
            new WaveFileWriter().Write(path, written, 22050);

            Assert.True(_loader.TryLoad(path, out LoadedWave wave));

            Assert.Equal(22050, wave.SampleRate);
            Assert.Equal(written.Length, wave.Samples.Length);
            Assert.Equal(0f, wave.Samples[0], 4);
            Assert.Equal(0.5f, wave.Samples[1], 4);
            Assert.Equal(-0.5f, wave.Samples[2], 4);
            Assert.Null(wave.LoopStart);
        }

        /// <summary>
        /// A loop point stored in a <c>smpl</c> chunk has to survive the walk over the chunks.
        /// </summary>
        /// <remarks>
        /// This is where the loop point of a converted tune is meant to live, so that it travels
        /// with the rendered file instead of having to be looked up somewhere else.
        /// <para>
        /// RIFF stores the last sample that is still played, the mixer turns around at the sample
        /// after it, so a stored 39 has to come back as 40.
        /// </para>
        /// </remarks>
        [Fact]
        public void ReadsTheLoopPointFromASamplerChunk()
        {
            byte[] bytes = BuildWaveWithLoop(sampleCount: 64, loopStart: 8, loopEnd: 39);

            Assert.True(_loader.TryParse(bytes, out LoadedWave wave));

            Assert.Equal(64, wave.Samples.Length);
            Assert.Equal(8, wave.LoopStart);
            Assert.Equal(40, wave.LoopEnd);
        }

        /// <summary>
        /// A loop point the writer stores has to come back out of the file unchanged.
        /// </summary>
        /// <remarks>
        /// This is the whole point of putting it in the file: the loop point travels with the
        /// rendered audio instead of being looked up somewhere else.
        /// </remarks>
        [Fact]
        public void ReadsBackTheLoopPointTheWriterStored()
        {
            short[] written = [.. Enumerable.Repeat((short)1000, 64)];
            string path = Path.Combine(_root, "looping.wav");
            new WaveFileWriter().Write(path, written, 44100, loopEndSample: 40);

            Assert.True(_loader.TryLoad(path, out LoadedWave wave));

            Assert.Equal(64, wave.Samples.Length);
            Assert.Equal(0, wave.LoopStart);
            Assert.Equal(40, wave.LoopEnd);
        }

        /// <summary>
        /// A loop point outside the audio is dropped rather than written, so a reader cannot be sent
        /// past the end of the data.
        /// </summary>
        [Fact]
        public void DropsALoopPointThatIsNotInsideTheAudio()
        {
            short[] written = [.. Enumerable.Repeat((short)1000, 32)];
            string path = Path.Combine(_root, "bad-loop.wav");
            new WaveFileWriter().Write(path, written, 44100, loopEndSample: 64);

            Assert.True(_loader.TryLoad(path, out LoadedWave wave));

            Assert.Equal(32, wave.Samples.Length);
            Assert.Null(wave.LoopStart);
            Assert.Null(wave.LoopEnd);
        }

        /// <summary>
        /// Anything that is not mono 16-bit PCM is refused rather than played as noise.
        /// </summary>
        [Fact]
        public void RefusesAFileThatIsNotMonoSixteenBitPcm()
        {
            byte[] bytes = BuildWaveWithLoop(sampleCount: 16, loopStart: 0, loopEnd: 8, channels: 2);

            Assert.False(_loader.TryParse(bytes, out _));
        }

        private static byte[] BuildWaveWithLoop(int sampleCount, int loopStart, int loopEnd,
            short channels = 1)
        {
            var data = new byte[sampleCount * 2];
            for (int index = 0; index < sampleCount; index++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(index * 2), (short)(index * 100));
            }

            var smpl = new byte[36 + 24];
            BinaryPrimitives.WriteInt32LittleEndian(smpl.AsSpan(28), 1);
            BinaryPrimitives.WriteInt32LittleEndian(smpl.AsSpan(36 + 8), loopStart);
            BinaryPrimitives.WriteInt32LittleEndian(smpl.AsSpan(36 + 12), loopEnd);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(4 + 24 + 8 + data.Length + 8 + smpl.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(44100);
            writer.Write(44100 * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(data.Length);
            writer.Write(data);

            writer.Write(Encoding.ASCII.GetBytes("smpl"));
            writer.Write(smpl.Length);
            writer.Write(smpl);

            writer.Flush();

            return stream.ToArray();
        }
    }
}
