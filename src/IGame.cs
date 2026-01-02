using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public interface IGame :
		IGameUnitsCommand, IGameUnitsQuery,
		IGameWonderQuery, IGamePlayerQuery,
		IGameTurnQuery, IGameSettings
	{
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
		IUnit CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false);
	}
	public interface IGameUnitsQuery
	{
		IUnit[] GetUnits();
		IUnit[] GetUnits(int x, int y);
	}

	public interface IGamePlayerQuery
	{
		Player GetPlayer(byte playerId);
	}

	public interface IGameWonderQuery
	{
		bool WonderObsolete<T>() where T : IWonder, new();
		bool WonderObsolete(IWonder wonder);
	}
}