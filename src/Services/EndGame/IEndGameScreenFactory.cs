using System;
using CivOne.Screens;
using CivOne.Screens.Dialogs;

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
		/// <summary>
		/// Creates the space-victory screen.
		/// </summary>
		/// <param name="text">The flight and landing lines.</param>
		/// <param name="rivalBackground">The human player's palace for a rival's arrival; <c>null</c> shows the space station.</param>
		/// <param name="onLanding">Called when the landing picture appears.</param>
		/// <param name="flightDuration">Length of the flight music, to time the landing to its end; <c>null</c> when unknown.</param>
		IScreen SpaceVictory(SpaceVictoryText text, PalaceData? rivalBackground, Action? onLanding, TimeSpan? flightDuration);

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

		/// <summary>
		/// Creates a newspaper with the given lines.
		/// </summary>
		IScreen Newspaper(string[] lines);

		/// <summary>
		/// Creates the conquest screen ("The entire world hails ...").
		/// </summary>
		IScreen Conquest();

		/// <summary>
		/// Creates the "Do you want to keep playing?" dialog.
		/// </summary>
		IKeepPlayingDialog KeepPlaying();
	}
}
