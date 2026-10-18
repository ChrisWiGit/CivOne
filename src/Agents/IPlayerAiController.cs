using System;
using CivOne.Units;

namespace CivOne.Agents
{
	/// <summary>
	/// Provides the legacy player AI actions used by non-human players.
	/// </summary>
	internal interface IPlayerAiController
	{
		/// <summary>
		/// Executes one movement decision for the specified unit.
		/// </summary>
		/// <param name="unit">The unit to move.</param>
		void Move(IUnit unit);

		/// <summary>
		/// Chooses the next production item for the specified city.
		/// </summary>
		/// <param name="city">The city whose production should be selected.</param>
		void CityProduction(City city);

		/// <summary>
		/// Selects the next research target for the current player.
		/// </summary>
		void ChooseResearch();
	}

	/// <summary>
	/// Creates per-player AI controller instances.
	/// </summary>
	internal interface IPlayerAiControllerFactory
	{
		/// <summary>
		/// Gets the controller that should manage the specified player.
		/// </summary>
		/// <param name="player">The player that needs an AI controller.</param>
		/// <returns>The controller assigned to the player.</returns>
		IPlayerAiController CreateFor(Player player);
	}
}
