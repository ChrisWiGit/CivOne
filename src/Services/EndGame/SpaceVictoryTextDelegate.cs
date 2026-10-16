using System;
using System.Globalization;
using System.Linq;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Builds the text of the space-victory screen from the game text (<c>KING.TXT</c>, section <c>*SPACE</c> for the
	/// human player, <c>*SPACE2</c> for a rival) and fills in the placeholders like the original game.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	/// <item><c>$US</c>: title and name of the human leader, also for a rival's arrival.</item>
	/// <item><c>$THEM</c>: nationality of the arriving player.</item>
	/// <item><c>$BUCKS</c>: colonists of the arriving ship in ten thousands; the text adds "0,000" itself.</item>
	/// <item><c>$RPLC1</c>: launch year; <c>$RPLC2</c>: arrival year. Both are plain numbers.</item>
	/// </list>
	/// The first <see cref="FlightLineCount"/> lines are shown during the flight, the others after the landing.
	/// </remarks>
	internal sealed class SpaceVictoryTextDelegate
	{
		/// <summary>
		/// The number of lines shown during the flight; the first one is the empty opening line.
		/// </summary>
		public const int FlightLineCount = 15;

		private const int PeoplePerColonistUnit = 10_000;

		/// <summary>
		/// Fills in the placeholders and splits the text.
		/// </summary>
		/// <param name="lines">The lines of the game text section.</param>
		/// <param name="humanLeader">Title and name of the human leader.</param>
		/// <param name="nationality">Nationality of the arriving player.</param>
		/// <param name="population">Population of the arriving ship (people).</param>
		/// <param name="launchYear">Launch year of the arriving ship.</param>
		/// <param name="arrivalYear">The year of the arrival.</param>
		/// <returns>The flight and landing lines.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public SpaceVictoryText Build(string[] lines, string humanLeader, string nationality, int population, int launchYear, int arrivalYear)
		{
			ArgumentNullException.ThrowIfNull(lines);

			string colonists = (population / PeoplePerColonistUnit).ToString(CultureInfo.InvariantCulture);
			string[] text = [.. lines.Select(line => line
				.Replace("$US", humanLeader, StringComparison.Ordinal)
				.Replace("$THEM", nationality, StringComparison.Ordinal)
				.Replace("$BUCKS", colonists, StringComparison.Ordinal)
				.Replace("$RPLC1", launchYear.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
				.Replace("$RPLC2", arrivalYear.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
				.Replace("^", "", StringComparison.Ordinal))];

			return new SpaceVictoryText([.. text.Take(FlightLineCount)], [.. text.Skip(FlightLineCount)]);
		}
	}
}
