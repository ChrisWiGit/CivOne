namespace CivOne.Sound.Engine;

/// <summary>
/// One request from the game thread to the mixer.
/// </summary>
/// <remarks>
/// The game thread never touches a voice. It queues a command, and the mixer applies it at the
/// start of its next pass. That way the audio thread needs no lock, which it must not take: a
/// blocked callback is an audible gap.
/// </remarks>
/// <param name="Kind">What to do.</param>
/// <param name="HandleId">Which voice it applies to, for the commands that address one.</param>
/// <param name="Bus">Which bus it applies to, for the commands that address one.</param>
/// <param name="Value">A volume, for the commands that set one.</param>
/// <param name="FadeSamples">How long the fade takes, in samples.</param>
/// <param name="Voice">The prepared voice, for <see cref="MixerCommandKind.Add"/>.</param>
internal readonly record struct MixerCommand(
	MixerCommandKind Kind,
	int HandleId = 0,
	SoundBus Bus = SoundBus.Effect,
	float Value = 0f,
	int FadeSamples = 0,
	MixerVoice? Voice = null)
{
	/// <summary>Adds a prepared voice.</summary>
	/// <param name="voice">The voice to add.</param>
	/// <returns>The command.</returns>
	public static MixerCommand Add(MixerVoice voice) => new(MixerCommandKind.Add, Voice: voice);

	/// <summary>Pauses a voice.</summary>
	/// <param name="handleId">Voice to pause.</param>
	/// <param name="fadeSamples">Length of the fade out before it freezes.</param>
	/// <returns>The command.</returns>
	public static MixerCommand Pause(int handleId, int fadeSamples)
		=> new(MixerCommandKind.Pause, handleId, FadeSamples: fadeSamples);

	/// <summary>Resumes a voice.</summary>
	/// <param name="handleId">Voice to resume.</param>
	/// <param name="fadeSamples">Length of the fade back up.</param>
	/// <returns>The command.</returns>
	public static MixerCommand Resume(int handleId, int fadeSamples)
		=> new(MixerCommandKind.Resume, handleId, FadeSamples: fadeSamples);

	/// <summary>Stops a voice.</summary>
	/// <param name="handleId">Voice to stop.</param>
	/// <param name="fadeSamples">Length of the fade out before it ends.</param>
	/// <returns>The command.</returns>
	public static MixerCommand Stop(int handleId, int fadeSamples)
		=> new(MixerCommandKind.Stop, handleId, FadeSamples: fadeSamples);

	/// <summary>Sets the volume of a voice.</summary>
	/// <param name="handleId">Voice to change.</param>
	/// <param name="volume">The new volume.</param>
	/// <returns>The command.</returns>
	public static MixerCommand SetVolume(int handleId, float volume)
		=> new(MixerCommandKind.SetVolume, handleId, Value: volume);

	/// <summary>Stops every voice on a bus.</summary>
	/// <param name="bus">Bus to clear.</param>
	/// <param name="fadeSamples">Length of the fade out.</param>
	/// <returns>The command.</returns>
	public static MixerCommand StopBus(SoundBus bus, int fadeSamples)
		=> new(MixerCommandKind.StopBus, Bus: bus, FadeSamples: fadeSamples);

	/// <summary>Sets the master volume of a bus.</summary>
	/// <param name="bus">Bus to change.</param>
	/// <param name="volume">The new volume.</param>
	/// <returns>The command.</returns>
	public static MixerCommand SetBusVolume(SoundBus bus, float volume)
		=> new(MixerCommandKind.SetBusVolume, Bus: bus, Value: volume);
}
