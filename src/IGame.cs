using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne
{
	public interface IGame
	{
		IUnit CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false);

		ushort GameTurn { get; }

		Player GetPlayer(byte playerId);
	}
}