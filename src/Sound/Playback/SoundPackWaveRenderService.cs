using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

internal sealed class SoundPackWaveRenderService
{
	private const int SoundSampleRate = 44100;
	private const short SoundAmplitude = 8000;

	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method is required for DI")]
	public string? Render(string packFolder, string fileName)
	{
		string sourcePath = Path.Combine(packFolder, fileName);
		if (!File.Exists(sourcePath)) return null;

		string cacheFolder = Path.Combine(packFolder, "wav-cache");
		Directory.CreateDirectory(cacheFolder);

		string targetPath = Path.Combine(cacheFolder, Path.ChangeExtension(fileName, ".wav"));
		if (File.Exists(targetPath) && File.GetLastWriteTimeUtc(targetPath) >= File.GetLastWriteTimeUtc(sourcePath)) return targetPath;

		TuneScorePack pack = TuneScoreJson.Load(sourcePath);
		TuneScore? tune = pack.Tunes.FirstOrDefault();
		if (tune == null || tune.Steps.Count == 0) return null;

		WriteWaveFile(targetPath, RenderPcm16(pack, tune));
		return targetPath;
	}

	private static short[] RenderPcm16(TuneScorePack pack, TuneScore tune)
	{
		List<short> samples = [];
		foreach (TuneStep step in tune.Steps)
		{
			int sampleCount = Math.Max(1, (int)Math.Round(pack.DurationSeconds(step) * SoundSampleRate));
			if (step.IsRest)
			{
				samples.AddRange(Enumerable.Repeat((short)0, sampleCount));
				continue;
			}

			double frequency = step.FrequencyHz(pack.PitClockHz);
			for (int i = 0; i < sampleCount; i++)
			{
				double phase = i * frequency / SoundSampleRate;
				samples.Add(phase % 1d < 0.5d ? SoundAmplitude : (short)-SoundAmplitude);
			}
		}

		return [.. samples];
	}

	private static void WriteWaveFile(string path, short[] samples)
	{
		const short CHANNELS = 1;
		const short BITS_PER_SAMPLE = 16;
		const short BYTES_PER_SAMPLE = BITS_PER_SAMPLE / 8;
		int dataSize = samples.Length * BYTES_PER_SAMPLE;

		using var stream = File.Create(path);
		using var writer = new BinaryWriter(stream, Encoding.ASCII);
		writer.Write(Encoding.ASCII.GetBytes("RIFF"));
		writer.Write(36 + dataSize); // = size of the rest of the file
		writer.Write(Encoding.ASCII.GetBytes("WAVE"));
		writer.Write(Encoding.ASCII.GetBytes("fmt "));
		writer.Write(16); // = size of the rest of the subchunk
		writer.Write((short)1);
		writer.Write(CHANNELS);
		writer.Write(SoundSampleRate);
		writer.Write(SoundSampleRate * CHANNELS * BYTES_PER_SAMPLE);
		writer.Write((short)(CHANNELS * BYTES_PER_SAMPLE));
		writer.Write(BITS_PER_SAMPLE);
		writer.Write(Encoding.ASCII.GetBytes("data"));
		writer.Write(dataSize);

		foreach (short sample in samples)
		{
			writer.Write(sample);
		}
	}
}