// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Advances;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	/// <summary>
	/// Defines the Game dependencies required by <see cref="Player"/>.
	/// Replaces the static <c>Game</c> field reference to allow testability.
	/// </summary>
	public interface IPlayerGame
	{
		bool Started { get; }
		ushort GameTurn { get; }
		int Difficulty { get; }

		Player HumanPlayer { get; }
		Player CurrentPlayer { get; }
		IEnumerable<Player> Players { get; }

		byte PlayerNumber(Player player);
		Player? GetPlayer(byte number);

		City[] GetCities();
		IUnit[] GetUnits();
		void DisbandUnit(IUnit? unit);

		bool WonderObsolete<T>() where T : IWonder, new();
		bool WonderBuilt<T>() where T : IWonder;
		IWonder[] BuiltWonders { get; }

		void SetAdvanceOrigin(IAdvance advance, Player player);
	}
}
