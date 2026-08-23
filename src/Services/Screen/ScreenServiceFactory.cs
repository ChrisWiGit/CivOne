// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.Screen
{
	/// <summary>
	/// Factory for creating screen services.
	/// </summary>
	/// <remarks>
	/// Centralizes construction so service implementations can be swapped without touching callers.
	/// </remarks>
	internal static class ScreenServiceFactory
	{
		/// <summary>
		/// Creates a default screen service that combines query and command operations.
		/// </summary>
		/// <returns>A service implementing both IScreenQueryService and IScreenCommandService.</returns>
		public static IScreenCommandService CreateCommandService() => new ScreenServiceImpl();

		/// <summary>
		/// Creates a default screen query service.
		/// </summary>
		/// <returns>A service implementing IScreenQueryService.</returns>
		public static IScreenQueryService CreateQueryService() => new ScreenServiceImpl();
	}
}
