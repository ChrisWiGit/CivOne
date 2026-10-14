using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Screens.Reports;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Default <see cref="IEndGameScreenFactory"/> creating the real game screens.
	/// </summary>
	internal sealed class EndGameScreenFactory : IEndGameScreenFactory
	{
		/// <inheritdoc />
		public IScreen Victory() => new VictoryScreen();

		/// <inheritdoc />
		public IScreen Defeat() => new DefeatScreen();

		/// <inheritdoc />
		public IScreen SpaceVictory(IPlayer winner) => new SpaceVictory(winner);

		/// <inheritdoc />
		public IScreen PowerGraph() => new Screens.PowerGraph();

		/// <inheritdoc />
		public IScreen CivilizationScore() => new Screens.Reports.CivilizationScore();

		/// <inheritdoc />
		public IScreen FamousPersons() => TopLeaderScreenFactory.Create();

		/// <inheritdoc />
		public IScreen HallOfFame() => HallOfFameScreenFactory.AddScore();

		/// <inheritdoc />
		public IScreen Credits() => new Screens.Credits();
	}
}
