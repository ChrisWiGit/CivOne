using System;
using System.Buffers.Binary;
using System.IO;

namespace CivOne.Sound.Playback;

/// <summary>
/// Reads the playing time of a wave file <see cref="WaveFileWriter"/> wrote: mono PCM behind a
/// fixed 44-byte header, nothing exotic to walk past.
/// </summary>
internal sealed class WaveFileDurationDelegate
{
    private const int HeaderSize = 44;
    private const int ChannelsOffset = 22;
    private const int SampleRateOffset = 24;
    private const int BitsPerSampleOffset = 34;
    private const int DataSizeOffset = 40;

    /// <summary>
    /// Tries to read how long a wave file plays for.
    /// </summary>
    /// <param name="path">Path of the wave file.</param>
    /// <param name="duration">The length, when the file could be read as a wave file.</param>
    /// <returns><c>true</c> when the duration is known.</returns>
    public bool TryRead(string path, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;

        Span<byte> header = stackalloc byte[HeaderSize];
        try
        {
            using (FileStream stream = File.OpenRead(path))
            {
                // A single Read may return less than asked for even away from the end of the file,
                // which would make a sound file look truncated. ReadExactly keeps reading, and
                // signals a real truncation with an EndOfStreamException - an IOException.
                stream.ReadExactly(header);
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        short channels = BinaryPrimitives.ReadInt16LittleEndian(header[ChannelsOffset..]);
        int sampleRate = BinaryPrimitives.ReadInt32LittleEndian(header[SampleRateOffset..]);
        short bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(header[BitsPerSampleOffset..]);
        int dataSize = BinaryPrimitives.ReadInt32LittleEndian(header[DataSizeOffset..]);

        int bytesPerFrame = channels * (bitsPerSample / 8);
        if (sampleRate <= 0 || bytesPerFrame <= 0) return false;

        duration = TimeSpan.FromSeconds((double)dataSize / (sampleRate * bytesPerFrame));
        return true;
    }
}
