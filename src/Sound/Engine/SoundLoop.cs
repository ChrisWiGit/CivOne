namespace CivOne.Sound.Engine;

/// <summary>
/// Whether and how a sound repeats when it reaches its end.
/// </summary>
internal enum SoundLoop
{
	/// <summary>Play once and end.</summary>
	None,

	/// <summary>
	/// Repeat, turning around at the file's own loop point when it has one and at the end of the
	/// file when it does not.
	/// </summary>
	Always,

	/// <summary>
	/// Repeat only when the file carries a loop point, otherwise play once.
	/// </summary>
	/// <remarks>
	/// This is the right choice for converted tunes. A renderer writes a loop point exactly when one
	/// of the tune's voices rewound while it was being rendered, which is the evidence that the
	/// original driver looped it. Three of the fourteen long leader themes end instead of looping,
	/// and those carry no loop point - repeating them at the end of the file would play on past an
	/// ending the composer wrote.
	/// </remarks>
	WhenMarked
}
