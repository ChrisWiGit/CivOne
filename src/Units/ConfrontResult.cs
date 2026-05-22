namespace CivOne.Units;

/// <summary>
/// Reason why a confrontation was blocked.
/// </summary>
public enum ConfrontBlockReason
{
    /// <summary>No block — confrontation was allowed.</summary>
    None,
    /// <summary>The Senate vetoed the attack under Democracy government.</summary>
    SenateBlockedAttack
}

/// <summary>
/// Result of a confrontation (attack/capture) operation.
/// Contains outcome flags and optional side effect data.
/// </summary>
public class ConfrontResult
{
    /// <summary>
    /// Whether the confrontation executed successfully (unit moved or combat occurred).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the confrontation was blocked (e.g., by Senate).
    /// </summary>
    public bool Blocked { get; set; }

    /// <summary>
    /// Whether a movement animation should be started.
    /// </summary>
    public bool MovementStarted { get; set; }

    /// <summary>
    /// Reason for blocking (if Blocked = true).
    /// </summary>
    public ConfrontBlockReason BlockReason { get; set; } = ConfrontBlockReason.None;

    /// <summary>
    /// Optional callback for post-execution side effects.
    /// Can be used for animations, audio, UI updates, etc.
    /// </summary>
    public System.Action<IConfrontContext> PostExecuteCallback { get; set; }
}
