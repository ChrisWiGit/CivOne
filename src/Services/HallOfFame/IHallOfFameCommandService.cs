using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Mutating commands for Hall of Fame storage (for example clearing persisted entries).
	/// </summary>
	internal interface IHallOfFameCommandService
	{
		/// <summary>
		/// Clears the Hall of Fame and returns the resulting persisted entries (usually current human entry only).
		/// </summary>
		IReadOnlyList<HallOfFameEntry> Clear();
	}
}
