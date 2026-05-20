using System;
using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	internal interface IHallOfFamePersistService
	{
		IReadOnlyList<HallOfFameEntry> ViewEntries(string storageDirectory, Action<string> log = null);

		IReadOnlyList<HallOfFameEntry> AddEntry(string storageDirectory, HallOfFameEntry entry, Action<string> log = null);
	}
}
