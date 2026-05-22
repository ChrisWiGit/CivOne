using CivOne.Tiles;

namespace CivOne.Units;

/// <summary>
/// Runtime context for a confrontation (unit movement/combat).
/// Passed to ConfrontDelegate.Execute() to provide call-time data and mutation callbacks.
/// </summary>
public interface IConfrontContext
{
    /// <summary>
    /// The unit initiating the attack/capture.
    /// </summary>
    IUnit AttackingUnit { get; }

    /// <summary>
    /// The target tile.
    /// </summary>
    ITile MoveTarget { get; }

    /// <summary>
    /// The unit on the target tile (if any).
    /// </summary>
    IUnit TargetUnit { get; }

    /// <summary>
    /// Remaining movement points for the attacker.
    /// </summary>
    byte MovesLeft { get; }

    /// <summary>
    /// Partial movement point for current move.
    /// </summary>
    byte PartMoves { get; }

    /// <summary>
    /// Whether the unit is currently fortified.
    /// </summary>
    bool Fortify { get; }

    /// <summary>
    /// Whether the fortification is active.
    /// </summary>
    bool FortifyActive { get; }

    /// <summary>
    /// Whether the attacking unit is a veteran (experienced).
    /// Can be set by combat outcomes.
    /// </summary>
    bool Veteran { get; set; }

    /// <summary>
    /// Whether the attacking unit is a Diplomat.
    /// </summary>
    bool IsAttackerDiplomat { get; }

    /// <summary>
    /// Whether the attacking unit is a Caravan.
    /// </summary>
    bool IsAttackerCaravan { get; }

    /// <summary>
    /// Whether the attacking unit is Nuclear.
    /// </summary>
    bool IsAttackerNuclear { get; }

    /// <summary>
    /// Whether the attacking unit is a cannon type (deals splash damage).
    /// </summary>
    bool IsAttackerCannonType { get; }

    /// <summary>
    /// Relative X offset from current tile to target tile.
    /// </summary>
    int RelX { get; }

    /// <summary>
    /// Relative Y offset from current tile to target tile.
    /// </summary>
    int RelY { get; }

    /// <summary>
    /// Callback to reduce attacker movement points.
    /// </summary>
    System.Action<int> ConsumeMoves { get; }

    /// <summary>
    /// Callback to clear the "Goto" order.
    /// </summary>
    System.Action OnGotoCleared { get; }

}
