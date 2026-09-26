namespace CivOne.Sound.Engine;

/// <summary>
/// Groups sounds that share a master volume and a replacement rule.
/// </summary>
internal enum SoundBus
{
	/// <summary>
	/// Background music.
	/// Usually one piece at a time, often looping.
	/// </summary>
	Music,

	/// <summary>
	/// Short sounds that play over the music.
	/// </summary>
	Effect
}
