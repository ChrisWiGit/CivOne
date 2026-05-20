using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	internal interface IHallOfFameFileRepository
	{
		bool TryLoad(string storageDirectory, out IReadOnlyList<HallOfFameEntry> entries, out string error);

		bool TrySave(string storageDirectory, IReadOnlyList<HallOfFameEntry> entries, out string error);
	}
}
