using CivOne.Civilizations;

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
    /// Get player by owner index.
    /// </summary>
    Player GetPlayer(byte owner);

    /// <summary>
    /// Check if two players are at war.
    /// </summary>
    bool IsAtWar(Player playerA, Player playerB);
}
