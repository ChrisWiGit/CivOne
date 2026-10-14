using CivOne.Persistence.Game;
using CivOne.Screens;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Creates the screens of the end-game sequences, so <see cref="EndGameService"/> only decides their order.
	/// </summary>
	internal interface IEndGameScreenFactory
	{
		/// <summary>Creates the conquest victory screen.</summary>
		/// <returns>A new screen.</returns>
		IScreen Victory();

		/// <summary>Creates the defeat screen.</summary>
		/// <returns>A new screen.</returns>
		IScreen Defeat();

		/// <summary>Creates the screen for a spaceship reaching Alpha Centauri.</summary>
		/// <param name="winner">The player whose ship arrived.</param>
		/// <returns>A new screen.</returns>
		IScreen SpaceVictory(IPlayer winner);

		/// <summary>Creates the power graph.</summary>
		/// <returns>A new screen.</returns>
		IScreen PowerGraph();

		/// <summary>Creates the civilization score screen.</summary>
		/// <returns>A new screen.</returns>
		IScreen CivilizationScore();

		/// <summary>Creates the Famous Persons screen.</summary>
		/// <returns>A new screen.</returns>
		IScreen FamousPersons();

		/// <summary>Creates the Hall of Fame screen after adding the current game.</summary>
		/// <returns>A new screen.</returns>
		IScreen HallOfFame();

		/// <summary>Creates the credits screen shown after the game has ended.</summary>
		/// <returns>A new screen.</returns>
		IScreen Credits();
	}
}
