using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public interface IGame
	{
		IUnit CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false);

		ushort GameTurn { get; }

		Player GetPlayer(byte playerId);

		IUnit[] GetUnits();

		int Difficulty { get; }
		int MaxDifficulty { get; }

		bool WonderObsolete<T>() where T : IWonder, new();
		bool WonderObsolete(IWonder wonder);

	}
}