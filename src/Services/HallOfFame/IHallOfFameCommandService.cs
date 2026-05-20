using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	internal interface IHallOfFameCommandService
	{
		IReadOnlyList<HallOfFameEntry> Clear();
	}
}
