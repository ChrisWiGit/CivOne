using CivOne.Services.Peace;
using CivOne.Services.Score;

namespace CivOne.Services
{
	/// <summary>
	/// Creates the civilization score service and the rules it is built from.
	/// </summary>
	internal static class CivilizationScoreServiceFactory
	{
		/// <summary>
		/// Creates the default civilization score service.
		/// </summary>
		/// <returns>A new <see cref="ICivilizationScoreService"/>.</returns>
		public static ICivilizationScoreService CreateDefault() => new CivilizationScoreService();

		/// <summary>
		/// Creates the service that turns a score into the rating and the Famous Person.
		/// </summary>
		/// <returns>A new <see cref="ICivilizationRatingService"/> following the original game.</returns>
		public static ICivilizationRatingService CreateRatingService() => new CivilizationRatingService();

		/// <summary>
		/// Creates the service that counts the peaceful turns for the peace term of the score.
		/// </summary>
		/// <returns>A new <see cref="IPeaceTurnService"/> starting at 0.</returns>
		public static IPeaceTurnService CreatePeaceTurnService() => new PeaceTurnService();
	}
}
