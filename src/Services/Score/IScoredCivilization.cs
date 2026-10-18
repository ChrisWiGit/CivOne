using System.Collections.Generic;

namespace CivOne.Services.Score
{
	/// <summary>
	/// The per-player state the civilization score reads.
	/// </summary>
	public interface IScoredCivilization
	{
		/// <summary>
		/// Gets the player's cities, with happiness measured at the 40 % luxury reference.
		/// </summary>
		IReadOnlyList<CityScoreInput> Cities { get; }

		/// <summary>
		/// Gets the number of wonders in the player's cities, obsolete wonders included.
		/// </summary>
		int WonderCount { get; }

		/// <summary>
		/// Gets the number of future technologies the player has researched.
		/// </summary>
		int FutureTechCount { get; }

		/// <summary>
		/// Gets the spaceship population in people (10,000 per habitation module).
		/// </summary>
		int SpaceShipPopulation { get; }

		/// <summary>
		/// Gets the live mission success rate of the player's spaceship in percent.
		/// </summary>
		int SpaceShipSuccessRate { get; }

		/// <summary>
		/// Gets whether the player's spaceship arrives in the current turn.
		/// </summary>
		bool SpaceShipArrivedThisTurn { get; }
	}
}
