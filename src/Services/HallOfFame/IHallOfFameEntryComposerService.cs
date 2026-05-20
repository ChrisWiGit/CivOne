namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Composes a <see cref="HallOfFameEntry"/> from the current game context for persistence.
	/// </summary>
	internal interface IHallOfFameEntryComposerService
	{
		/// <summary>
		/// Compose a Hall of Fame entry representing the current human player's performance.
		/// </summary>
		HallOfFameEntry ComposeForHuman();
	}
}
