namespace CivOne.Sound.Engine;

/// <summary>
/// What a <see cref="MixerCommand"/> asks the mixer to do.
/// </summary>
internal enum MixerCommandKind
{
	/// <summary>Add a prepared voice and start playing it.</summary>
	Add,

	/// <summary>Fade a voice out and freeze it where it is.</summary>
	Pause,

	/// <summary>Unfreeze a voice and fade it back up.</summary>
	Resume,

	/// <summary>Fade a voice out and remove it.</summary>
	Stop,

	/// <summary>Set the volume of a voice.</summary>
	SetVolume,

	/// <summary>Fade out and remove every voice on a bus.</summary>
	StopBus,

	/// <summary>Set the master volume of a bus.</summary>
	SetBusVolume
}
