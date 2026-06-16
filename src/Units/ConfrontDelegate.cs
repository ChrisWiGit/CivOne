using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Tiles;
using System.Diagnostics;
using System.Linq;

namespace CivOne.Units;

/// <summary>
/// Encapsulates Senate guard check for confrontation (attack/capture) logic.
/// Separates government rule logic from unit movement and animation.
/// All external dependencies are injected for testability.
/// </summary>
/// <remarks>
/// Create a new confrontation delegate with injected services.
/// </remarks>
internal class ConfrontDelegate(IConfrontGameServices gameServices)
{
    private readonly IConfrontGameServices _gameServices = gameServices;

	/// <summary>
	/// Check if confrontation is allowed under Democracy government.
	/// Senate blocks attacks on non-hostile, non-Barbarian civilizations.
	/// Only affects the human player under Democracy.
	/// </summary>
	public bool AllowedToConfrontInDemocracy(BaseUnit attackingUnit, ITile moveTarget)
    {
        if (attackingUnit == null)
            return true;

        Player? attackerOwner = _gameServices.GetPlayer(attackingUnit.Owner);

        if (attackerOwner == null)
        {
            Debug.Assert(false, "Attacking unit has no valid owner");
            return true;
        }
        
        if (!attackerOwner.IsHuman)
            return true;

        if (attackerOwner.Government is not Democracy)
            return true;

        Player? targetOwner = GetConfrontationTargetOwner(moveTarget);
        if (targetOwner == null)
            return true;

        if (targetOwner.Civilization is Barbarian)
            return true;

        if (_gameServices.IsAtWar(attackerOwner, targetOwner))
            return true;

        return false;
    }

    /// <summary>
    /// Resolve the owner of the target (from city or unit).
    /// </summary>
    private Player? GetConfrontationTargetOwner(ITile moveTarget)
    {
		if (moveTarget?.Units.FirstOrDefault() is BaseUnit targetUnit)
			return _gameServices.GetPlayer(targetUnit.Owner);

		var targetCity = moveTarget?.City;
        if (targetCity != null)
            return _gameServices.GetPlayer(targetCity.CityOwnerPlayerIndex);

        return null;
    }
}
