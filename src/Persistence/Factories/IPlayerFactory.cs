using CivOne.Civilizations;
using CivOne.Persistence.Game;

namespace CivOne.Persistence.Model
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
