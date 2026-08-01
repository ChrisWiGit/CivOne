// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Creates the <see cref="IStartPositionService"/> implementation selected by <see cref="Settings.StartPositionAlgorithm"/>.
	/// </summary>
	internal static class StartPositionServiceFactory
	{
		/// <summary>
		/// Creates the service for the algorithm currently configured in the settings.
		/// </summary>
		/// <returns>The starting-position service to use.</returns>
		public static IStartPositionService Create() => Create(Settings.Instance.StartPositionAlgorithm);

		/// <summary>
		/// Creates the service for a specific algorithm, without reading the settings singleton.
		/// </summary>
		/// <param name="algorithm">The algorithm to create a service for.</param>
		/// <returns>The starting-position service for that algorithm.</returns>
		public static IStartPositionService Create(Settings.StartPositionAlgorithmType algorithm) => algorithm switch
		{
			Settings.StartPositionAlgorithmType.AreaBased => new AreaBasedStartPositionService(),
			_ => new LegacyStartPositionService(),
		};
	}
}
