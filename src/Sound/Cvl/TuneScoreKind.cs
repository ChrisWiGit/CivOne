namespace CivOne.Sound.Cvl;

/// <summary>
/// How a tune is realized inside the driver.
/// </summary>
internal enum TuneScoreKind
{
    /// <summary>
    /// The handler is not a tune sequence (stop, status query, special-case logic).
    /// </summary>
    Unsupported,

    /// <summary>
    /// The handler returns immediately - the tune is deliberately empty in the driver.
    /// </summary>
    Silent,

    /// <summary>
    /// Music sequence: 4-byte records of {timbre, duration, PIT divisor}.
    /// </summary>
    Music,

    /// <summary>
    /// Effect sequence: 10-byte records with their own noise mask and slide parameters.
    /// </summary>
    Effect
}
