// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Services.Screen;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Factory for creating end-game services.
	/// </summary>
	/// <remarks>
	/// Centralizes construction so service implementations can be swapped without touching callers.
	/// </remarks>
	internal static class EndGameServiceFactory
	{
		/// <summary>
		/// Creates the default <see cref="IEndGameService"/>.
		/// </summary>
		/// <returns>A fully configured <see cref="IEndGameService"/>.</returns>
		public static IEndGameService CreateDefault()
		{
			return new EndGameService(
				screenCommand: ScreenServiceFactory.CreateCommandService(),
				screenQuery: ScreenServiceFactory.CreateQueryService(),
				gameService: GameServiceFactory.CreateDefault());
		}
	}
}
