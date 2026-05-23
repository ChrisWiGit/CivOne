using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.src;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class ConfrontDelegateTests : TestsBase
	{
		private readonly ConfrontDelegate _testee;
		private readonly Player _humanPlayer;
		private readonly Player _enemyPlayer;

		public ConfrontDelegateTests()
		{
			_testee = new ConfrontDelegate(new ConfrontGameServicesAdapter());
			_humanPlayer = Game.Instance.HumanPlayer;
			_enemyPlayer = GetNonBarbarianPlayers(1)[0];
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsFalse_ForHumanDemocracyAttackingPeacefulUnit()
		{
			// Arrange
			_humanPlayer.Government = new Democracy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 40, 30, _humanPlayer);
			ITile moveTarget = CreateUnit(UnitType.Militia, 41, 30, _enemyPlayer).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.False(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsTrue_ForHumanNonDemocracyAttacker()
		{
			// Arrange
			_humanPlayer.Government = new Monarchy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 42, 30, _humanPlayer);
			ITile moveTarget = CreateUnit(UnitType.Militia, 43, 30, _enemyPlayer).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsTrue_ForAiDemocracyAttacker()
		{
			// Arrange
			_enemyPlayer.Government = new Democracy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 44, 30, _enemyPlayer);
			ITile moveTarget = CreateUnit(UnitType.Militia, 45, 30, _humanPlayer).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsTrue_ForBarbarianTarget()
		{
			// Arrange
			_humanPlayer.Government = new Democracy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 46, 30, _humanPlayer);
			ITile moveTarget = CreateUnit(UnitType.Militia, 47, 30, Game.Instance.GetPlayer(0)).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsTrue_ForAtWarTarget()
		{
			// Arrange
			_humanPlayer.Government = new Democracy();
			_humanPlayer.DeclareWar(_enemyPlayer);
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 48, 30, _humanPlayer);
			ITile moveTarget = CreateUnit(UnitType.Militia, 49, 30, _enemyPlayer).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsFalse_ForPeacefulEnemyCity()
		{
			// Arrange
			_humanPlayer.Government = new Democracy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 50, 30, _humanPlayer);
			ITile moveTarget = Game.Instance.AddCity(_enemyPlayer, 0, 51, 30).Tile;

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.False(actual);
		}

		[Fact]
		public void AllowedToConfrontInDemocracy_ReturnsTrue_WhenTargetHasNoOwner()
		{
			// Arrange
			_humanPlayer.Government = new Democracy();
			BaseUnit attackingUnit = CreateUnit(UnitType.Chariot, 52, 30, _humanPlayer);
			ITile moveTarget = Map.Instance[53, 30];

			// Act
			bool actual = _testee.AllowedToConfrontInDemocracy(attackingUnit, moveTarget);

			// Assert
			Assert.True(actual);
		}

		private static Player[] GetNonBarbarianPlayers(int count)
			=> Game.Instance.Players.Where(player => player != null && !player.IsHuman && player.Civilization is not Barbarian).Take(count).ToArray();

		private static BaseUnit CreateUnit(UnitType unitType, int x, int y, Player owner)
			=> (BaseUnit)Game.Instance.CreateUnit(unitType, x, y, Game.Instance.PlayerNumber(owner));
	}
}