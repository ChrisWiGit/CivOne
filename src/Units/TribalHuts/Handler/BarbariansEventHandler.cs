using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Units.TribalHuts
{
	internal class BarbariansEventHandler : ITribalHutEventHandler
	{
		private readonly int x;
		private readonly int y;
		private readonly Map map;

		private readonly Random random;

		private readonly IGame gameInstance;

		public BarbariansEventHandler(int x, int y, IGame gameInstance, Map map, Random random)
		{
			this.random = random;
			this.x = x;
			this.y = y;
			this.map = map;
			this.gameInstance = gameInstance;
		}

		public string[] GetEventMessage()
		{
			return
			[
				"You have unleashed",
				"a horde of barbarians!"
			];
		}

		public void PreExecute()
		{
			//TODO: Find out how the barbarians should be created
			// This implementation is an approximation
			int count = 0;
			for (int i = 0; i < 1000; i++)
			{
				foreach (ITile tile in map[x, y].GetBorderTiles())
				{
					bool hasCity = tile.City != null;
					bool hasUnits = tile.Units.Length > 0;
					bool isOcean = tile.IsOcean;

					if (hasCity || hasUnits || isOcean) continue;

					if (random.Next(0, 10) < 6) continue;

					gameInstance.CreateUnit(GetRandomBarbarianUnitType(), tile.X, tile.Y, 0, true);


					count++;
				}
				if (count > 0) break;
			}
		}

		private UnitType GetRandomBarbarianUnitType()
		{
			return random.Next(0, 100) < 50 ? UnitType.Cavalry : UnitType.Legion;
		}

		public void PostExecute()
		{
			// No post-execution logic needed for this event
		}
	}
}
