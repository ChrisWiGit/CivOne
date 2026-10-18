namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Debug launch rules that only require a player and an unset launch year.
	/// </summary>
	public class DebugSpaceShipLaunchRules : SpaceShipLaunchRules
	{
		public override bool CanLaunch(IPlayerSpaceRace player)
		{
			return player != null && player.SpaceShipLaunchYear == 0;
		}
	}
}