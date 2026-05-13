// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	public class SpaceShipService(
		IPlayerSpaceRace player,
		ISpaceShipPlacementRules placementRules,
		ISpaceShipLaunchRules launchRules,
		ISpaceShipScreenDataFactory screenDataFactory) : ISpaceShipService
	{
		private readonly IPlayerSpaceRace _player = player;
		private readonly ISpaceShipPlacementRules _placementRules = placementRules;
		private readonly ISpaceShipLaunchRules _launchRules = launchRules;
		private readonly ISpaceShipScreenDataFactory _screenDataFactory = screenDataFactory;

		private void TryAutoAddCommandModule()
		{
			SpaceShipPartCounts counts = SpaceShipPartCounter.Count(_player.SpaceShipGrid);
			if (counts.CommandModule > 0)
			{
				return;
			}

			if ((counts.LifeSupportModule + counts.HabitationModule) < 3)
			{
				return;
			}

			_placementRules.TryAddPart(_player, SpaceShipComponentType.CommandModule);
		}

		public bool CanAddAnyPart()
		{
			foreach (SpaceShipComponentType partType in System.Enum.GetValues(typeof(SpaceShipComponentType)))
			{
				if (partType == SpaceShipComponentType.Empty)
				{
					continue;
				}

				if (CanAddPart(partType))
				{
					return true;
				}
			}

			return false;
		}

		public bool CanAddPart(SpaceShipComponentType partType)
		{
			if (partType == SpaceShipComponentType.Empty)
			{
				return false;
			}

			return _placementRules.CanAddPart(_player, partType);
		}

		public bool TryAddPart(SpaceShipComponentType partType)
		{
			if (partType == SpaceShipComponentType.Empty)
			{
				return false;
			}

			bool inserted = _placementRules.TryAddPart(_player, partType);
			if (!inserted)
			{
				return false;
			}

			TryAutoAddCommandModule();

			SpaceShipScreenData data = GetScreenData();
			_player.SpaceShipPopulation = (ushort)System.Math.Max(0, data.Population);
			return true;
		}

		public bool CanLaunch() => _launchRules.CanLaunch(_player);

		public SpaceShipScreenData GetScreenData() => _screenDataFactory.Create(_player, CanLaunch());
	}
}
