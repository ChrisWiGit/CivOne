// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Factory for creating game services.
	/// </summary>
	/// <remarks>
	/// Centralizes construction so service implementations can be swapped without touching callers.
	/// </remarks>
	internal static class GameServiceFactory
	{
		/// <summary>
		/// Creates a default game service.
		/// </summary>
		/// <returns>A service implementing IGameService.</returns>
		public static IGameService CreateDefault() => new GameServiceImpl();
	}
}
