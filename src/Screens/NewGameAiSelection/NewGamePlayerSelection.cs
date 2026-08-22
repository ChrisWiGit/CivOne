// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Civilizations;

namespace CivOne.Screens
{
	/// <summary>
	/// Represents one configured player row in the selection screen.
	/// </summary>
	internal sealed class NewGamePlayerSelection
	{
		/// <summary>
		/// Gets a value indicating whether this row represents the human player.
		/// </summary>
		public required bool IsHuman { get; init; }

		/// <summary>
		/// Gets or sets the display name for the player.
		/// </summary>
		public required string Name { get; set; }

		/// <summary>
		/// Gets or sets the selected civilization.
		/// </summary>
		public required ICivilization Civilization { get; set; }

		/// <summary>
		/// Gets or sets the selected AI identifier.
		/// </summary>
		public required Guid? AiId { get; set; }

		/// <summary>
		/// Gets or sets the selected AI display name.
		/// </summary>
		public required string AiName { get; set; }

		/// <summary>
		/// Gets or sets the selected AI difficulty index.
		/// </summary>
		public required int DifficultyIndex { get; set; }

		/// <summary>
		/// Gets or sets the selected color slot.
		/// </summary>
		public required int ColorSlot { get; set; }

		/// <summary>
		/// Gets or sets the selected team slot.
		/// </summary>
		public required int TeamSlot { get; set; }
	}
}