using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public interface IGame :
		IGameUnitsCommand, IGameUnitsQuery,
		IGameWonderQuery, IGamePlayerQuery,
		IGameTurnQuery, IGameSettings, IGameBarbarianSettings
	{
	}

	/// <summary>
	/// Tells consumers where barbarians may come from in the running game.
	/// </summary>
	public interface IGameBarbarianSettings
	{
		/// <summary>
		/// Gets the barbarian sources active in this game.
		/// </summary>
		BarbarianActivity BarbarianActivity { get; }
	}

	public interface IGameTurnQuery
	{
		ushort GameTurn { get; }
	}
	public interface IGameSettings
	{
		int Difficulty { get; }
		int MaxDifficulty { get; }
	}

	public interface IGameUnitsCommand
	{
		IUnit? CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false);
	}
	public interface IGameUnitsQuery
	{
		IUnit[] GetUnits();
		IUnit[]? GetUnits(int x, int y);
	}

	public interface IGamePlayerQuery
	{
		Player? GetPlayer(byte playerId);
	}

	/// <summary>
	/// Answers questions about the cities of the running game that a single city cannot answer itself.
	/// </summary>
	public interface IGameCityQuery
	{
		/// <summary>
		/// Gets the position of <paramref name="city"/> in the game's city list.
		/// This is the equivalent of the original game's numeric city slot.
		/// The value is derived from the list on every call and never persisted, because the map editor
		/// can add and remove cities at arbitrary positions.
		/// </summary>
		/// <param name="city">The city to look up.</param>
		/// <returns>The zero based position, or -1 when the city is not part of the game.</returns>
		int GetCityIndex(ICityBasic city);

		/// <summary>
		/// Tells whether the given player is the human player.
		/// The original game applies a different base unhappiness term to the human player than to its
		/// computer opponents.
		/// </summary>
		/// <param name="playerIndex">The player to check.</param>
		/// <returns><c>true</c> when the player is the human player.</returns>
		bool IsHumanPlayer(byte playerIndex);
	}

	public interface IGameWonderQuery
	{
		bool WonderObsolete<T>() where T : IWonder, new();
		bool WonderObsolete(IWonder wonder);
	}
}