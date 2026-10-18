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
    private const int ChunkHeaderSize = 8;

    /// <summary>Size of a <c>smpl</c> chunk that carries exactly one loop.</summary>
    private const int SamplerChunkSize = 36 + 24;

    /// <summary>
    /// Writes samples to a wave file, creating the folder if needed.
    /// </summary>
    /// <param name="path">Where to write.</param>
    /// <param name="samples">The samples to write.</param>
    /// <param name="sampleRate">Rate of the samples, in Hz.</param>
    /// <param name="loopEndSample">
    /// Where a looping tune turns around, written as a <c>smpl</c> chunk, or <c>null</c> for a tune
    /// that does not loop.
    /// </param>
    /// <remarks>
    /// The loop point is stored in the file rather than beside it so that it travels with the
    /// rendered audio. It cannot live in the pack index: the index is written while the pack is
    /// converted, long before anything is rendered, and it is different for every arrangement.
    /// </remarks>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a writer used as an instance, not a static utility.")]
    public void Write(string path, short[] samples, int sampleRate, int? loopEndSample = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        int dataSize = samples.Length * BytesPerSample;
        bool writeLoop = loopEndSample is > 0 && loopEndSample <= samples.Length;
        int loopChunkSize = writeLoop ? ChunkHeaderSize + SamplerChunkSize : 0;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(HeaderSize + dataSize + loopChunkSize);
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

        if (writeLoop) WriteSamplerChunk(writer, sampleRate, loopEndSample!.Value);
    }

    /// <summary>
    /// Writes the <c>smpl</c> chunk that carries the loop point.
    /// </summary>
    /// <param name="writer">Writer positioned at the end of the file.</param>
    /// <param name="sampleRate">Rate of the samples, in Hz.</param>
    /// <param name="loopEndSample">Sample the loop turns around at.</param>
    /// <remarks>
    /// Only one loop is written, running from the start of the file, which is all a tune needs.
    /// Everything the chunk says about the sampler itself is left at zero; readers that care about
    /// loops only look at the loop list.
    /// <para>
    /// RIFF counts the loop end as the last sample that is still played, while the engine turns
    /// around <em>at</em> its loop end, so one is taken off here. A player that knows nothing about
    /// CivOne then loops at the same place the game does.
    /// </para>
    /// </remarks>
    private static void WriteSamplerChunk(BinaryWriter writer, int sampleRate, int loopEndSample)
    {
        const int NanosecondsPerSecond = 1_000_000_000;

        writer.Write(Encoding.ASCII.GetBytes("smpl"));
        writer.Write(SamplerChunkSize);

        writer.Write(0);                                    // manufacturer
        writer.Write(0);                                    // product
        writer.Write(NanosecondsPerSecond / sampleRate);    // sample period, in nanoseconds
        writer.Write(60);                                   // MIDI unity note
        writer.Write(0);                                    // MIDI pitch fraction
        writer.Write(0);                                    // SMPTE format
        writer.Write(0);                                    // SMPTE offset
        writer.Write(1);                                    // number of loops
        writer.Write(0);                                    // sampler data

        writer.Write(0);                                    // loop identifier
        writer.Write(0);                                    // loop type: forward
        writer.Write(0);                                    // loop start
        writer.Write(loopEndSample - 1);                    // loop end, inclusive
        writer.Write(0);                                    // fraction
        writer.Write(0);                                    // play count: endless
    }
}
