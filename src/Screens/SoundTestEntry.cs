using CivOne.Sound.Cvl;

namespace CivOne.Screens
{
	/// <summary>
	/// One line of the sound test.
	/// </summary>
	/// <remarks>
	/// The test offers two rather different things under the same menu: the tunes of a converted
	/// sound pack, which are described by that pack's index, and the sounds a collection of wave
	/// files covers, which have no index at all. Both are reduced to a name and a title here, so the
	/// menu does not have to know which of the two it is showing.
	/// </remarks>
	/// <param name="Name">Name the sound is played by.</param>
	/// <param name="Title">Title to show, already translated.</param>
	/// <param name="PackEntry">
	/// The pack's own entry, or <c>null</c> when this sound does not come from a pack. A pack entry
	/// is played directly, so the test plays that pack even when the game is set to something else.
	/// </param>
	internal readonly record struct SoundTestEntry(string Name, string Title, SoundPackIndexEntry? PackEntry);
}
