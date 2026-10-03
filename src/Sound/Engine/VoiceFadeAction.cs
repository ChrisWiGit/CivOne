namespace CivOne.Sound.Engine;

/// <summary>
/// What the mixer does with a voice once its fade has reached the gain it was heading for.
/// </summary>
internal enum VoiceFadeAction
{
	/// <summary>Keep playing at the volume the fade arrived at.</summary>
	None,

	/// <summary>Freeze the voice where it is.</summary>
	Pause,

	/// <summary>End the voice and remove it.</summary>
	Stop
}
