using CivOne.Civilizations;
using CivOne.Enums;

namespace CivOne.Units;

/// <summary>
/// Game services specific to confrontation logic.
/// Abstracts dependencies from the Game class for dependency injection.
/// 
/// This interface is designed for future dependency injection refactoring to abstract
/// core game services needed by confrontation logic. Implementing this interface allows
/// alternative game service implementations and facilitates testing with mock services.
/// </summary>
public interface IConfrontGameServices
{
    /// <summary>
    /// Register a hostile action between players.
    /// </summary>
    void RegisterHostileAction();

    /// <summary>
    /// Get player by owner index.
    /// </summary>
    Player GetPlayer(byte owner);

    /// <summary>
    /// Check if two players are at war.
    /// </summary>
    bool IsAtWar(Player playerA, Player playerB);

    /// <summary>
    /// Current game difficulty level.
    /// </summary>
    int Difficulty { get; }

    /// <summary>
    /// Whether the game has started.
    /// </summary>
    bool Started { get; }
}
