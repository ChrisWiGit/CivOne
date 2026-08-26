using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace CivOne.Sound.Playback;



/// <summary>
/// Writes mono 16-bit PCM to a RIFF wave file, the format the runtime hands to the audio backend.
/// </summary>
internal sealed class WaveFileWriter
{
    private const short Channels = 1;
    private const short BitsPerSample = 16;
    private const short BytesPerSample = BitsPerSample / 8;
    private const short PcmFormat = 1;
    private const int HeaderSize = 36;
    private const int FormatChunkSize = 16;

    /// <summary>
    /// Writes samples to a wave file, creating the folder if needed.
    /// </summary>
    /// <param name="path">Where to write.</param>
    /// <param name="samples">The samples to write.</param>
    /// <param name="sampleRate">Rate of the samples, in Hz.</param>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a writer used as an instance, not a static utility.")]
    public void Write(string path, short[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        int dataSize = samples.Length * BytesPerSample;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(HeaderSize + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(FormatChunkSize);
        writer.Write(PcmFormat);
        writer.Write(Channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * Channels * BytesPerSample);
        writer.Write((short)(Channels * BytesPerSample));
        writer.Write(BitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (short sample in samples)
        {
            writer.Write(sample);
        }
    }
}
