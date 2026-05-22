using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Screens.Dialogs;
using CivOne.Tiles;

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
	/// Execute Senate guard check and return result.
	/// Returns Blocked=true if confrontation is not allowed.
	/// </summary>
	public ConfrontResult Execute(IConfrontContext context)
    {
        var result = new ConfrontResult();

        if (!AllowedToConfrontInDemocracy(context))
        {
            result.Blocked = true;
            result.BlockReason = ConfrontBlockReason.SenateBlockedAttack;
            return result;
        }

        // No restrictions - proceed with confrontation
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Check if confrontation is allowed under Democracy government.
    /// Senate blocks attacks on non-hostile, non-Barbarian civilizations.
    /// Only affects the human player under Democracy.
    /// </summary>
    private bool AllowedToConfrontInDemocracy(IConfrontContext context)
    {
        var attackingUnit = context.AttackingUnit as BaseUnit;
        if (attackingUnit == null)
            return true;

        Player attackerOwner = _gameServices.GetPlayer(attackingUnit.Owner);
        
        if (!attackerOwner.IsHuman)
            return true;

        if (attackerOwner.Government is not Democracy)
            return true;

        Player targetOwner = GetConfrontationTargetOwner(context);
        if (targetOwner == null)
            return true;  // No target owner - no one to attack

        if (targetOwner.Civilization is Barbarian)
            return true;

        if (_gameServices.IsAtWar(attackerOwner, targetOwner))
            return true;

        return false;
    }

    /// <summary>
    /// Resolve the owner of the target (from city or unit).
    /// </summary>
    private Player GetConfrontationTargetOwner(IConfrontContext context)
    {
        var targetUnit = context.TargetUnit as BaseUnit;
        if (targetUnit != null)
            return _gameServices.GetPlayer(targetUnit.Owner);

        var targetCity = context.MoveTarget?.City;
        if (targetCity != null)
            return _gameServices.GetPlayer(targetCity.Owner);

        return null;
    }
}
