using CivOne.Governments;
using CivOne.Services.EndGame;
using CivOne.Services.SpaceShip;
using CivOne.Tasks;

namespace CivOne
{
	/// <summary>
	/// End-of-game state and the end-of-turn check of the game.
	/// </summary>
	public partial class Game : IEndOfGameState
	{
		private IGameEndCheckService? _gameEndCheckService;

		/// <summary>
		/// Gets or sets whether the final score was recorded and the player chose to keep playing.
		/// </summary>
		/// <remarks>
		/// Like the original game, the latch is set after the end-game flow (power graph, score, Famous Persons,
		/// Hall of Fame). While it is set, no further end-game flow runs, no Hall of Fame entry is made, the palace is
		/// no longer upgraded and the score screen shows "SCORING COMPLETED".
		/// </remarks>
		internal bool ScoringCompleted { get; set; }

		bool IEndOfGameState.ScoringCompleted
		{
			get => ScoringCompleted;
			set => ScoringCompleted = value;
		}

		private IGameEndCheckService GameEndCheckService => _gameEndCheckService ??= EndGameServiceFactory.CreateCheckService();

		/// <summary>
		/// Checks once per turn change whether the retirement warning is due or the game ends.
		/// The end-game flow runs as a game task, so the next turn's tasks wait for "keep playing?".
		/// </summary>
		private void EnqueueGameEndCheck()
		{
			Player? human = HumanPlayer;
			if (human == null)
			{
				return;
			}

			// The conquest latch is set when the last rival is destroyed (see PlayerDestroyed); this only reads it.
			GameEndCheckResult result = GameEndCheckService.Check(_players, GameTurn, Difficulty, IsWorldConquered());

			if (result.RetirementWarning)
			{
				string title = new LeaderTitleDelegate(Translation).Title(human.Government);
				GameTask.Enqueue(Message.Newspaper(null, TranslateFormattedArray("{0} {1}\nplans retirement\nin 20 years.", title, human.LeaderName)));
			}

			if (!result.EndsGame)
			{
				return;
			}

			GameTask.Enqueue(CreateGameEndTask(result, human));
		}

		/// <summary>
		/// Creates the end-game flow for a spaceship arrival of <paramref name="winner"/> this turn.
		/// Used by the debug menu.
		/// </summary>
		/// <param name="winner">The player whose spaceship arrives.</param>
		/// <returns>The task running the flow.</returns>
		internal GameEndTask CreateSpaceArrivalEndTask(Player winner)
		{
			GameEndCheckResult result = new(RetirementWarning: false, Conquest: false, SpaceshipWinnerSlot: PlayerNumber(winner), YearEnd: false);
			return CreateGameEndTask(result, HumanPlayer);
		}

		private GameEndTask CreateGameEndTask(GameEndCheckResult result, Player human)
		{
			GameEndSequence sequence = BuildGameEndSequence(result, human);
			return new GameEndTask(() => EndGameServiceFactory.CreateForHuman().HandleGameEndAsync(sequence));
		}

		private GameEndSequence BuildGameEndSequence(GameEndCheckResult result, Player human)
		{
			Player? winner = result.HasSpaceshipWinner ? _players[result.SpaceshipWinnerSlot] : null;

			return new GameEndSequence
			{
				ConquestNews = result.Conquest ? TranslateArray("Your civilization\nhas conquered\nthe entire planet!") : null,
				SpaceVictoryText = winner != null ? BuildSpaceVictoryText(winner, human) : null,
				SpaceshipWinnerIsHuman = winner == human,
				HumanPalace = human.Palace,
				SpaceshipNews = winner != null ? TranslateFormattedArray("{0} spaceship\narrives at Alpha Centauri.", winner.TribeName) : null,
				YearEndNews = result.YearEnd ? TranslateFormattedArray("{0} dynasty\nends after glorious\n6000 year reign!", human.LeaderName) : null,
				YearEndTune = result.YearEnd ? human.Civilization.Tune : null,
			};
		}

		private SpaceVictoryText BuildSpaceVictoryText(Player winner, Player human)
		{
			string[] lines = GetGameText(winner == human ? "KING/SPACE" : "KING/SPACE2");
			string humanLeader = new LeaderTitleDelegate(Translation).Title(human.Government) + " " + human.LeaderName;
			int population = SpaceShipServiceFactoryProvider.GetInstance().Create(winner).GetScreenData().Population;
			return new SpaceVictoryTextDelegate().Build(lines, humanLeader, winner.TribeName, population, winner.SpaceShipLaunchYear, CalendarService.TurnToYear(GameTurn));
		}
	}
}
