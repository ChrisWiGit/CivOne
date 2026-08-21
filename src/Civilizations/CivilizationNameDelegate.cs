// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CivOne.Civilizations
{
	/// <summary>
	/// The display names for one player, or <see langword="null"/> entries when the civilization's own
	/// names can be used unchanged.
	/// </summary>
	/// <param name="LeaderName">The leader name to use, or <see langword="null"/> for the default.</param>
	/// <param name="TribeName">The tribe name to use, or <see langword="null"/> for the default.</param>
	/// <param name="TribeNamePlural">The plural tribe name to use, or <see langword="null"/> for the default.</param>
	internal readonly record struct CivilizationNames(string? LeaderName, string? TribeName, string? TribeNamePlural);

	/// <summary>
	/// Builds the display names for a player, disambiguating civilizations that are used by more than one
	/// player at a time ("Caesar II of the Romans II").
	///
	/// Needed once there are more non-barbarian players than the 14 available civilizations, and when a
	/// destroyed player respawns into a civilization that is already taken.
	/// </summary>
	internal sealed class CivilizationNameDelegate
	{
		private static readonly (int Value, string Numeral)[] RomanNumerals =
		[
			(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
			(100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
			(10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
		];

		/// <summary>
		/// Builds the names for a player that is about to be created.
		/// </summary>
		/// <param name="civilization">The civilization assigned to the player.</param>
		/// <param name="occurrence">
		/// How many other players already use this civilization. 0 means the player is the only one, and the
		/// civilization's own names are used unchanged.
		/// </param>
		/// <returns>The names to pass to the <see cref="Player"/> constructor.</returns>
		public CivilizationNames Build(ICivilization civilization, int occurrence)
		{
			ArgumentNullException.ThrowIfNull(civilization);

			if (occurrence <= 0)
			{
				return new CivilizationNames(null, null, null);
			}

			string numeral = ToRomanNumeral(occurrence + 1);
			return new CivilizationNames(
				$"{civilization.Leader.Name} {numeral}",
				$"{civilization.Name} {numeral}",
				$"{civilization.NamePlural} {numeral}");
		}

		/// <summary>
		/// Converts a small positive integer to a Roman numeral.
		/// </summary>
		/// <param name="number">The number to convert. Values below 1 produce an empty string.</param>
		/// <returns>The Roman numeral for <paramref name="number"/>.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public string ToRomanNumeral(int number)
		{
			StringBuilder numeral = new();
			foreach ((int value, string symbol) in RomanNumerals)
			{
				while (number >= value)
				{
					numeral.Append(symbol);
					number -= value;
				}
			}
			return numeral.ToString();
		}
	}
}
