namespace CivOne.UnitTests
{
	using System.Linq;
	using CivOne.Civilizations;
	using CivOne.src;
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

		[Fact]
		public void DeclareWar_WhenTradingCitiesExistBetweenParties_PurgesThemOnBothSides()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var attacker = players[0];
			var defender = players[1];
			City attackerCity = Game.Instance.AddCity(attacker, 0, 40, 30);
			City defenderCity = Game.Instance.AddCity(defender, 1, 42, 30);

			attackerCity.AddTradingCity(defenderCity);
			defenderCity.AddTradingCity(attackerCity);

			// Act
			attacker.DeclareWar(defender);

			// Assert
			Assert.DoesNotContain(defenderCity, attackerCity.TradingCitiesAsCity);
			Assert.DoesNotContain(attackerCity, defenderCity.TradingCitiesAsCity);
		}

		[Fact]
		public void DeclareWar_WhenThirdPartyTradingCitiesExist_LeavesThoseUntouched()
		{
			// Arrange
			var players = GetNonBarbarianPlayers(Game.Instance, 2);
			var attacker = players[0];
			var defender = players[1];
			var thirdParty = Game.Instance.GetPlayer(0);
			City attackerCity = Game.Instance.AddCity(attacker, 2, 44, 30);
			City defenderCity = Game.Instance.AddCity(defender, 3, 46, 30);
			City thirdPartyCity = Game.Instance.AddCity(thirdParty, 4, 48, 30);

			attackerCity.AddTradingCity(defenderCity);
			attackerCity.AddTradingCity(thirdPartyCity);
			defenderCity.AddTradingCity(attackerCity);
			defenderCity.AddTradingCity(thirdPartyCity);

			// Act
			attacker.DeclareWar(defender);

			// Assert
			Assert.DoesNotContain(defenderCity, attackerCity.TradingCitiesAsCity);
			Assert.DoesNotContain(attackerCity, defenderCity.TradingCitiesAsCity);
			Assert.Contains(thirdPartyCity, attackerCity.TradingCitiesAsCity);
			Assert.Contains(thirdPartyCity, defenderCity.TradingCitiesAsCity);
		}

		private static Player[] GetNonBarbarianPlayers(Game game, int count)
		{
			var players = Enumerable.Range(0, game.Players.Count())
				.Select(index => game.GetPlayer((byte)index))
				.Where(player => player != null && player.Civilization is not Barbarian)
				.Take(count)
				.ToArray();

			Assert.True(players.Length >= count, $"Expected at least {count} non-barbarian players in test setup.");
			return players;
		}
	}
}
