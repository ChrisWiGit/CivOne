using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Abstracts file operations for Hall of Fame YAML storage.
	/// </summary>
	internal interface IHallOfFameFileRepository
	{
		/// <summary>
		/// Attempts to load persisted entries from the given storage directory.
		/// </summary>
		bool TryLoad(string? storageDirectory, out IReadOnlyList<HallOfFameEntry> entries, out string? error);

		/// <summary>
		/// Attempts to save the provided entries to the storage directory.
		/// </summary>
		bool TrySave(string? storageDirectory, IReadOnlyList<HallOfFameEntry> entries, out string? error);
	}
}
