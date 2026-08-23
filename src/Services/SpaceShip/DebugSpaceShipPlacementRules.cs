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
	/// <summary>
	/// Debug variant of <see cref="SpaceShipPlacementRules"/> that allows placing any non-empty part type.
	/// </summary>
	public class DebugSpaceShipPlacementRules : SpaceShipPlacementRules
	{
		public DebugSpaceShipPlacementRules(ISpaceShipSlotBlueprint slotBlueprint) : base(slotBlueprint)
		{
		}

		public override bool CanAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType)
		{
			return player != null && partType != SpaceShipComponentType.Empty;
		}
	}
}
