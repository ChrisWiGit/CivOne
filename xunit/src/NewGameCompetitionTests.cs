using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the mapping between the opponent count offered by the New Game menu and the number of
	/// non-barbarian players a game is created with, and the game state that mapping produces.
	///
	/// The menu stores opponents, <see cref="Game.CreateGame"/> expects the human player plus the opponents.
	/// Passing the opponent count straight through produced a game without any opponent, which satisfies the
	/// "conquered the entire planet" condition on the first end of turn: the map came up with a starting
	/// settler that never became active and swallowed every input.
	/// </summary>
	public class NewGameCompetitionTests : IDisposable
	{
		private readonly MockRuntime _runtime;

		/// <summary>
		/// Sets up a runtime and the Earth map, so games can be created without the graphics subsystem.
		/// </summary>
		public NewGameCompetitionTests()
		{
			TranslationServiceFactory.ResetForTests();
			_runtime = new MockRuntime(new RuntimeSettings { InitialSeed = 24601 });
			Map.Reset(new MapGenerationFromYaml());
			Map.Instance.LoadEarthMapInThread();
		}

		private static void CreateGame(int competition, string civilizationName = "Babylonian")
			=> Game.CreateGame(3, competition, Common.Civilizations.First(c => c.Name == civilizationName), replaceExisting: true);


		/// <summary>
		/// Releases the game, map and runtime this test class set up.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Map.Reset();
				Game.Wipe();
				GameTask.ClearAll();
				_runtime.Dispose();
				RuntimeHandler.Wipe();
				GC.Collect();
			}
		}

		/// <summary>
		/// One opponent means three player slots: the barbarians, one AI player and the human player.
		/// </summary>
		[Fact]
		public void OneOpponentGivesOneHumanOneAiAndTheBarbarians()
		{
			CreateGame(2);

			Game game = Game.Instance;
			Assert.Equal(3, game.Players.Count());
			Assert.IsType<Barbarian>(game.GetPlayer(0)!.Civilization);

			Player[] regularPlayers = [.. Enumerable.Range(1, 2).Select(i => game.GetPlayer((byte)i)!)];
			Assert.Single(regularPlayers, player => player.IsHuman);
			Assert.Single(regularPlayers, player => !player.IsHuman);
			Assert.DoesNotContain(regularPlayers, player => player.Civilization is Barbarian);
		}

		/// <summary>
		/// The human player never occupies the barbarian slot, whichever civilization is picked.
		/// Slot 0 gets no starting units and no regular turn, so a human player there could never move.
		/// </summary>
		/// <param name="civilizationName">The civilization the human player picks.</param>
		[Theory]
		[InlineData("Babylonian")]
		[InlineData("Mongol")]
		[InlineData("Japanese")]
		public void TheHumanPlayerNeverHoldsTheBarbarianSlot(string civilizationName)
		{
			CreateGame(2, civilizationName);

			Game game = Game.Instance;
			byte humanSlot = game.PlayerNumber(game.HumanPlayer);
			Assert.NotEqual(0, humanSlot);
			Assert.Contains(game.GetUnits(), unit => unit.Owner == humanSlot);
		}

		/// <summary>
		/// Plays the turn loop forward until the human player is up.
		/// This is the regression test for the soft lock: with no opponent the conquest tasks were queued
		/// before the human player's first turn, and the starting unit never became active.
		/// </summary>
		/// <param name="competition">The number of non-barbarian players.</param>
		[Theory]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(8)]
		public void TheHumanPlayerReachesItsTurnWithAnActiveUnit(int competition)
		{
			CreateGame(competition);

			Game game = Game.Instance;
			for (int step = 0; step < 10000; step++)
			{
				if (game.CurrentPlayer == game.HumanPlayer && !GameTask.Any())
				{
					Assert.NotNull(game.ActiveUnit);
					Assert.Equal(game.PlayerNumber(game.HumanPlayer), game.ActiveUnit!.Owner);
					return;
				}

				if (GameTask.Any())
				{
					GameTask.Update();
					continue;
				}

				game.Update();
			}

			Assert.Fail($"The human player never got a turn (current slot {game.PlayerNumber(game.CurrentPlayer)}, {GameTask.HowMany()} pending tasks).");
		}

		/// <summary>
		/// A game needs at least one opponent. Without one, the human player has already conquered the
		/// planet before the first turn.
		/// </summary>
		[Fact]
		public void AGameWithoutAnyOpponentIsRejected()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => CreateGame(Game.MinCompetition - 1));
		}

		/// <summary>
		/// The barbarians hold slot 0, so a game cannot have more non-barbarian players than the remaining
		/// slots. Larger values would silently drop tile visibility and player colours.
		/// </summary>
		[Fact]
		public void MorePlayersThanSlotsAreRejected()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => CreateGame(Game.MaxCompetition + 1));
		}

		/// <summary>
		/// The barbarian civilization has no player slot of its own and cannot be played.
		/// </summary>
		[Fact]
		public void TheBarbarianCivilizationCannotBePlayed()
		{
			ICivilization barbarians = Common.Civilizations.First(c => c is Barbarian);
			Assert.Throws<ArgumentException>(() => Game.CreateGame(3, 7, barbarians, replaceExisting: true));
		}
	}
}
