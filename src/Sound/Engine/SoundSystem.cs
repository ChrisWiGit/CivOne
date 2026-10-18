using System;
using CivOne.Sound.Dsp;

namespace CivOne.Sound.Engine;

/// <summary>
/// The sound system: loads wave files, hands them to the mixer and owns the output device.
/// </summary>
/// <remarks>
/// Everything here runs on the game thread. The only thing that crosses to the audio thread is the
/// mixer's command queue, which is why this class never locks.
/// </remarks>
internal sealed class SoundSystem : ISoundSystem
{
	private readonly IAudioDevice _device;
	private readonly SoundMixer _mixer;
	private readonly WaveSampleLoaderDelegate _loader = new();
	private readonly AudioResamplerDelegate _resampler = new();

	private bool _disposed;

	/// <summary>
	/// Creates the sound system and starts its device.
	/// </summary>
	/// <param name="device">The output device.</param>
	/// <remarks>
	/// A device that could not be opened leaves <see cref="IsRunning"/> at <c>false</c>. The system
	/// is then useless and the caller has to let it go rather than keep it: everything queued on it
	/// would wait for a callback that never comes.
	/// </remarks>
	public SoundSystem(IAudioDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		_device = device;
		_mixer = new SoundMixer();
		IsRunning = _device.Start(_mixer.Render);
	}

	/// <summary>
	/// Gets whether the output was opened and is asking for samples.
	/// </summary>
	public bool IsRunning { get; }

	/// <inheritdoc />
	public ISoundHandle? Play(SoundRequest request)
	{
		if (!IsRunning) return null;
		if (string.IsNullOrEmpty(request.FilePath)) return null;
		if (!_loader.TryLoad(request.FilePath, out LoadedWave wave)) return null;

		float[] samples = wave.SampleRate == _device.SampleRate
			? wave.Samples
			: _resampler.Resample(wave.Samples, wave.SampleRate, _device.SampleRate);

		double rateScale = (double)_device.SampleRate / wave.SampleRate;

		var handle = new SoundHandle(_mixer.NextHandleId(), _mixer, _device.SampleRate);
		float volume = Math.Clamp(request.Volume, 0f, 1f);

		int fadeIn = ToSamples(request.Replaces != null && request.CrossFade > TimeSpan.Zero
			? request.CrossFade
			: request.FadeIn);

		var voice = new MixerVoice
		{
			Samples = samples,
			Handle = handle,
			Bus = request.Bus,
			Position = 0,
			Loop = ShouldLoop(request, wave),
			LoopStart = Scale(LoopStart(request, wave), rateScale, samples.Length),
			LoopEnd = Scale(LoopEnd(request, wave), rateScale, samples.Length),
			LoopCrossFade = ToSamples(request.LoopCrossFade),
			Volume = volume,
			Gain = fadeIn > 0 ? 0f : volume,
			TargetGain = volume
		};

		if (fadeIn > 0) voice.FadeTo(volume, fadeIn, VoiceFadeAction.None);

		_mixer.Post(MixerCommand.Add(voice));

		request.Replaces?.Stop(request.CrossFade);

		return handle;
	}

	/// <inheritdoc />
	public void StopAll(SoundBus bus, TimeSpan fade = default)
		=> _mixer.Post(MixerCommand.StopBus(bus, ToSamples(fade)));

	/// <inheritdoc />
	public void SetBusVolume(SoundBus bus, float volume)
		=> _mixer.Post(MixerCommand.SetBusVolume(bus, volume));

	/// <inheritdoc />
	public void Process()
	{
		while (_mixer.TryTakeEnded(out SoundHandle? handle))
		{
			handle?.RaiseEnded();
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed) return;

		// Stopping first is what makes this safe: it returns only once the callback cannot run
		// again, so nothing is reading the sample buffers while they are let go.
		_device.Stop();
		_device.Dispose();
		_disposed = true;
	}

	/// <summary>
	/// Decides whether a sound repeats.
	/// </summary>
	/// <param name="request">What the caller asked for.</param>
	/// <param name="wave">The file that was loaded.</param>
	/// <returns><c>true</c> when the voice turns around instead of ending.</returns>
	private static bool ShouldLoop(SoundRequest request, LoadedWave wave) => request.Loop switch
	{
		SoundLoop.Always => true,
		SoundLoop.WhenMarked => request.LoopEndSample != null || wave.LoopEnd != null,
		_ => false
	};

	/// <summary>
	/// Works out where a loop turns around.
	/// </summary>
	/// <remarks>
	/// The caller decides, the file's own loop point is the fallback, and the end of the data is the
	/// last resort. For a converted tune the end of the data is the wrong place - the render runs
	/// until every voice has wrapped once, so its tail already repeats the beginning - which is why
	/// the loop point travels with the file.
	/// </remarks>
	private static int LoopEnd(SoundRequest request, LoadedWave wave)
		=> request.LoopEndSample ?? wave.LoopEnd ?? wave.Samples.Length;

	/// <summary>
	/// Works out where a loop returns to, the same way <see cref="LoopEnd"/> does.
	/// </summary>
	private static int LoopStart(SoundRequest request, LoadedWave wave)
		=> request.LoopStartSample > 0 ? request.LoopStartSample : wave.LoopStart ?? 0;

	private static int Scale(int sample, double rateScale, int length)
		=> Math.Clamp((int)Math.Round(sample * rateScale), 0, length);

	private int ToSamples(TimeSpan span)
		=> span <= TimeSpan.Zero ? 0 : (int)Math.Round(span.TotalSeconds * _device.SampleRate);
}
