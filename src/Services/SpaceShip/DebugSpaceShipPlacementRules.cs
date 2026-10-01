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