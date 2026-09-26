using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CivOne.Sound.Engine;

/// <summary>
/// Reads a wave file into the samples the mixer works with.
/// </summary>
/// <remarks>
/// The mixer needs floating point samples it can scale per sample, so the file is converted once on
/// loading rather than being handed to the audio backend as it is.
/// <para>
/// Chunks are walked rather than assumed at fixed offsets, so a file that carries a loop point in a
/// <c>smpl</c> chunk reads correctly. Only mono 16-bit PCM is supported, which is what
/// <see cref="Playback.WaveFileWriter"/> produces.
/// </para>
/// </remarks>
internal sealed class WaveSampleLoaderDelegate
{
	private const int RiffHeaderSize = 12;
	private const int ChunkHeaderSize = 8;
	private const short PcmFormat = 1;
	private const float FullScale = 32768f;

	/// <summary>
	/// Reads a wave file.
	/// </summary>
	/// <param name="path">Path of the file.</param>
	/// <param name="wave">The samples and what is known about them.</param>
	/// <returns><c>true</c> when the file could be read as mono 16-bit PCM.</returns>
	public bool TryLoad(string path, out LoadedWave wave)
	{
		wave = default;

		byte[] bytes;
		try
		{
			bytes = File.ReadAllBytes(path);
		}
		catch (IOException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}

		return TryParse(bytes, out wave);
	}

	/// <summary>
	/// Reads a wave file that is already in memory.
	/// </summary>
	/// <param name="bytes">The file contents.</param>
	/// <param name="wave">The samples and what is known about them.</param>
	/// <returns><c>true</c> when the bytes could be read as mono 16-bit PCM.</returns>
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
	public bool TryParse(ReadOnlySpan<byte> bytes, out LoadedWave wave)
	{
		wave = default;

		if (bytes.Length < RiffHeaderSize) return false;
		if (!Matches(bytes, "RIFF") || !Matches(bytes[8..], "WAVE")) return false;

		int sampleRate = 0;
		short channels = 0;
		short bitsPerSample = 0;
		float[]? samples = null;
		int? loopStart = null;
		int? loopEnd = null;

		int offset = RiffHeaderSize;
		while (offset + ChunkHeaderSize <= bytes.Length)
		{
			ReadOnlySpan<byte> id = bytes.Slice(offset, 4);
			int size = BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + 4)..]);
			int body = offset + ChunkHeaderSize;

			if (size < 0 || body + size > bytes.Length) break;

			if (Matches(id, "fmt ") && size >= 16)
			{
				if (BinaryPrimitives.ReadInt16LittleEndian(bytes[body..]) != PcmFormat) return false;

				channels = BinaryPrimitives.ReadInt16LittleEndian(bytes[(body + 2)..]);
				sampleRate = BinaryPrimitives.ReadInt32LittleEndian(bytes[(body + 4)..]);
				bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(bytes[(body + 14)..]);
			}
			else if (Matches(id, "data"))
			{
				samples = ToSamples(bytes.Slice(body, size));
			}
			else if (Matches(id, "smpl"))
			{
				ReadLoopPoint(bytes.Slice(body, size), ref loopStart, ref loopEnd);
			}

			// Chunks are padded to an even length, and the pad byte is not counted in the size.
			offset = body + size + (size & 1);
		}

		if (samples == null || channels != 1 || bitsPerSample != 16 || sampleRate <= 0) return false;

		wave = new LoadedWave(samples, sampleRate, loopStart, loopEnd);
		return true;
	}

	private static float[] ToSamples(ReadOnlySpan<byte> data)
	{
		var samples = new float[data.Length / 2];

		for (int index = 0; index < samples.Length; index++)
		{
			samples[index] = BinaryPrimitives.ReadInt16LittleEndian(data[(index * 2)..]) / FullScale;
		}

		return samples;
	}

	/// <summary>
	/// Reads the first loop of a <c>smpl</c> chunk.
	/// </summary>
	/// <remarks>
	/// The chunk begins with nine words of information about the sampler itself, then the number of
	/// loops, then the loops. Only the first loop is used, and only its start and end.
	/// </remarks>
	private static void ReadLoopPoint(ReadOnlySpan<byte> body, ref int? loopStart, ref int? loopEnd)
	{
		const int LoopCountOffset = 28;
		const int FirstLoopOffset = 36;
		const int LoopStartOffset = FirstLoopOffset + 8;
		const int LoopEndOffset = FirstLoopOffset + 12;

		if (body.Length < LoopEndOffset + 4) return;
		if (BinaryPrimitives.ReadInt32LittleEndian(body[LoopCountOffset..]) < 1) return;

		int start = BinaryPrimitives.ReadInt32LittleEndian(body[LoopStartOffset..]);
		int end = BinaryPrimitives.ReadInt32LittleEndian(body[LoopEndOffset..]);

		if (start < 0 || end <= start) return;

		loopStart = start;
		loopEnd = end;
	}

	private static bool Matches(ReadOnlySpan<byte> bytes, string id)
	{
		if (bytes.Length < id.Length) return false;

		for (int index = 0; index < id.Length; index++)
		{
			if (bytes[index] != (byte)id[index]) return false;
		}

		return true;
	}
}
