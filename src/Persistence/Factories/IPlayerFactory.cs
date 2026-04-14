using CivOne.Civilizations;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Factories
{
	public interface IPlayerFactory
	{
		/// <summary>
		/// Creates or reassigns a player to a civilization.
		/// </summary>
		/// <param name="civilization"></param>
		/// <returns></returns>
		IPlayerRestorable Create(ICivilization civilization, PlayerDto dto);
	}
}
