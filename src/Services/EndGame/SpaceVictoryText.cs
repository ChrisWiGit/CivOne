using System.Collections.Generic;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// The text of the space-victory screen, split like the original game shows it.
	/// </summary>
	/// <param name="FlightLines">The lines shown one after another during the flight to Alpha Centauri.</param>
	/// <param name="LandingLines">The lines shown one after another after the landing.</param>
	internal sealed record SpaceVictoryText(IReadOnlyList<string> FlightLines, IReadOnlyList<string> LandingLines);
}
