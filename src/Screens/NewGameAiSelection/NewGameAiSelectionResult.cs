// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;

namespace CivOne.Screens
{
	/// <summary>
	/// Represents the configured player setup for starting a new game.
	/// </summary>
	internal sealed class NewGameAiSelectionResult
	{
		/// <summary>
		/// Gets the selected game difficulty.
		/// </summary>
		public required int Difficulty { get; init; }

		/// <summary>
		/// Gets the selected competition level.
		/// </summary>
		public required int Competition { get; init; }

		/// <summary>
		/// Gets the configured human player.
		/// </summary>
		public required NewGamePlayerSelection Human { get; init; }

		/// <summary>
		/// Gets the configured AI opponents.
		/// </summary>
		public required IReadOnlyList<NewGamePlayerSelection> Opponents { get; init; }
	}

	/// <summary>
	/// Provides event data for a new game start request.
	/// </summary>
	/// <param name="result">The configured game setup.</param>
	internal sealed class NewGameAiSelectionResultEventArgs(NewGameAiSelectionResult result) : EventArgs
	{
		/// <summary>
		/// Gets the configured game setup.
		/// </summary>
		public NewGameAiSelectionResult Result { get; } = result;
	}
}