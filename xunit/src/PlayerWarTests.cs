namespace CivOne.src
{
	using System.Linq;
	using CivOne.Civilizations;
	using Xunit;

	public class PlayerWarTests : TestsBase
	{
		[Fact]
		public void IsAtWar_WhenSetAtWarWasNeverCalled_ReturnsFalse()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var player = players[0];
			var enemy = players[1];

			// Act
			var actual = player.IsAtWar(enemy);

			// Assert
			Assert.False(actual);
		}

		[Fact]
		public void SetAtWar_WhenCalledTwiceWithTrue_RemainsAtWar()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var player = players[0];
			var enemy = players[1];
			var enemyNumber = Game.Instance.PlayerNumber(enemy);

			// Act
			player.SetAtWar(enemyNumber, true);
			player.SetAtWar(enemyNumber, true);
			var actual = player.IsAtWar(enemy);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void DeclareWar_WhenCalled_SetsSymmetricWarState()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var attacker = players[0];
			var defender = players[1];

			// Act
			attacker.DeclareWar(defender);
			var attackerView = attacker.IsAtWar(defender);
			var defenderView = defender.IsAtWar(attacker);

			// Assert
			Assert.True(attackerView);
			Assert.True(defenderView);
		}

		[Fact]
		public void MakePeace_AfterDeclareWar_ClearsSymmetricWarState()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var playerA = players[0];
			var playerB = players[1];
			playerA.DeclareWar(playerB);

			// Act
			playerA.MakePeace(playerB);
			var aView = playerA.IsAtWar(playerB);
			var bView = playerB.IsAtWar(playerA);

			// Assert
			Assert.False(aView);
			Assert.False(bView);
		}

		[Fact]
		public void DeclareWar_WhenEnemyIsBarbarian_DoesNotCreateFormalWarState()
		{
			// Arrange
			var player = GetNonBarbarianPlayers(Game.Instance, 1).Single();
			var barbarian = Game.Instance.GetPlayer(0);

			// Act
			player.DeclareWar(barbarian);
			var actual = player.IsAtWar(barbarian);

			// Assert
			Assert.False(actual);
		}

		private static Player[] GetNonBarbarianPlayers(Game game, int count)
		{
			var players = Enumerable.Range(0, 8)
				.Select(index => game.GetPlayer((byte)index))
				.Where(player => player != null && player.Civilization is not Barbarian)
				.Take(count)
				.ToArray();

			Assert.True(players.Length >= count, $"Expected at least {count} non-barbarian players in test setup.");
			return players;
		}
	}
}
