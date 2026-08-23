// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
