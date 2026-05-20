using System;
using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Persistence abstraction for Hall of Fame operations (viewing and adding entries).
	/// </summary>
	internal interface IHallOfFamePersistService
	{
		/// <summary>
		/// Returns the normalized list of Hall of Fame entries from storage.
		/// </summary>
		IReadOnlyList<HallOfFameEntry> ViewEntries(string storageDirectory, Action<string> log = null);

		/// <summary>
		/// Adds a new entry and persists the updated list.
		/// </summary>
		IReadOnlyList<HallOfFameEntry> AddEntry(string storageDirectory, HallOfFameEntry entry, Action<string> log = null);
	}
}
