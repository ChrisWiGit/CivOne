using System.Collections.Generic;

namespace CivOne.Services.Screen
{
	/// <summary>
	/// Keeps track of which civilizations the power graph draws.
	/// </summary>
	/// <remarks>
	/// The power graph gives every civilization its own row of 8 pixels, so a game with many players would
	/// draw most of its rows past the bottom of the screen.
	/// This service caps the number of drawn civilizations and remembers the choice while the game runs,
	/// so reopening the graph keeps the previous selection.
	///
	/// Players are identified by their player number (their slot in the game), not by object reference,
	/// so a civilization that is destroyed and respawns into the same slot keeps its selection.
	/// </remarks>
	internal interface IPowerGraphSelectionService
	{
		/// <summary>
		/// The highest number of civilizations that can be drawn at the same time.
		/// </summary>
		int MaxVisiblePlayers { get; }

		/// <summary>
		/// The number of civilizations that are selected by default.
		/// </summary>
		int DefaultVisiblePlayers { get; }

		/// <summary>
		/// Returns whether the given candidates need a selection at all.
		/// </summary>
		/// <param name="candidates">Player numbers of every civilization the graph could draw.</param>
		/// <returns><see langword="true"/> when there are more candidates than <see cref="MaxVisiblePlayers"/>.</returns>
		bool RequiresSelection(IReadOnlyList<int> candidates);

		/// <summary>
		/// Returns the player numbers the graph should draw, in candidate order.
		/// </summary>
		/// <param name="candidates">Player numbers of every civilization the graph could draw.</param>
		/// <param name="humanPlayerNumber">
		/// Player number of the human player, which is always part of the default selection.
		/// </param>
		/// <returns>
		/// All candidates when they fit, otherwise the selected ones, at most <see cref="MaxVisiblePlayers"/>.
		/// </returns>
		IReadOnlyList<int> GetVisiblePlayers(IReadOnlyList<int> candidates, int humanPlayerNumber);

		/// <summary>
		/// Returns whether the given player is currently selected.
		/// </summary>
		/// <param name="playerNumber">Player number to check.</param>
		/// <returns><see langword="true"/> when the player is drawn by the graph.</returns>
		bool IsSelected(int playerNumber);

		/// <summary>
		/// The number of currently selected players.
		/// </summary>
		int SelectedCount { get; }

		/// <summary>
		/// Selects or deselects a player.
		/// </summary>
		/// <param name="playerNumber">Player number to toggle.</param>
		/// <returns>
		/// <see langword="false"/> when the player could not be selected because
		/// <see cref="MaxVisiblePlayers"/> is already reached, otherwise <see langword="true"/>.
		/// </returns>
		bool Toggle(int playerNumber);
	}
}
