using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Default launch validation logic supporting both legacy and detailed spaceship part models.
	/// </summary>
	public class SpaceShipLaunchRules : ISpaceShipLaunchRules
	{
		private static bool CanLaunchLegacy(SpaceShipPartCounts counts)
		{
			return counts.Structural >= 4 && counts.Component >= 2 && counts.Module >= 1;
		}

		private static bool CanLaunchDetailed(SpaceShipPartCounts counts)
		{
			return counts.CommandModule >= 1
				&& counts.HabitationModule >= 1
				&& counts.LifeSupportModule >= 1
				&& counts.PropulsionComponent >= 2
				&& counts.FuelComponent >= 2
				&& counts.StructuralTotal > 0;
		}

		public virtual bool CanLaunch(IPlayerSpaceRace player)
		{
			if (player == null || player.SpaceShipLaunchYear != 0)
			{
				return false;
			}

			SpaceShipPartCounts counts = SpaceShipPartCounter.Count(player.SpaceShipGrid);
			if (counts.DetailedPartCount > 0)
			{
				return CanLaunchDetailed(counts);
			}

			return CanLaunchLegacy(counts);
		}
	}
}