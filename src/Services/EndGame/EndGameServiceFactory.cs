using CivOne.Persistence.Game;
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
		public static IPlayer Human => Game.Instance.HumanPlayer;
		/// <summary>
		/// Creates the default <see cref="IEndGameService"/>.
		/// </summary>
		/// <returns>A fully configured <see cref="IEndGameService"/>.</returns>
		public static IEndGameService CreateForHuman()
		{
			return new EndGameService(
				screenCommand: ScreenServiceFactory.CreateCommandService(),
				screenQuery: ScreenServiceFactory.CreateQueryService(),
				gameService: GameServiceFactory.CreateDefault(),
				winner: Human);
		}

		/// <summary>
		/// Creates the factory for the screens of the end-game sequences.
		/// </summary>
		/// <returns>A new <see cref="IEndGameScreenFactory"/> creating the real game screens.</returns>
		public static IEndGameScreenFactory CreateScreenFactory() => new EndGameScreenFactory();
	}
}