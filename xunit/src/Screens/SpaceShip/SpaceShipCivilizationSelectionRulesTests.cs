using CivOne.Enums;
using CivOne.Screens;
using Xunit;

namespace CivOne.UnitTests.Screens.SpaceShip
{
	public class SpaceShipCivilizationSelectionRulesTests
	{
		[Fact]
		public void IsEnabled_NoApolloAndNoParts_ReturnsFalse()
		{
			// Arrange
			SpaceShipComponentType[,] grid = new SpaceShipComponentType[12, 12];

			// Act
			bool actual = SpaceShipCivilizationSelectionRules.IsEnabled(hasApolloProgram: false, grid);

			// Assert
			Assert.False(actual);
		}

		[Fact]
		public void IsEnabled_HasApolloAndNoParts_ReturnsTrue()
		{
			// Arrange
			SpaceShipComponentType[,] grid = new SpaceShipComponentType[12, 12];

			// Act
			bool actual = SpaceShipCivilizationSelectionRules.IsEnabled(hasApolloProgram: true, grid);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void IsEnabled_NoApolloAndHasPart_ReturnsTrue()
		{
			// Arrange
			SpaceShipComponentType[,] grid = new SpaceShipComponentType[12, 12];
			grid[4, 7] = SpaceShipComponentType.Structural;

			// Act
			bool actual = SpaceShipCivilizationSelectionRules.IsEnabled(hasApolloProgram: false, grid);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void HasAnySpaceShipPart_NullGrid_ReturnsFalse()
		{
			// Arrange
			SpaceShipComponentType[,] grid = null;

			// Act
			bool actual = SpaceShipCivilizationSelectionRules.HasAnySpaceShipPart(grid);

			// Assert
			Assert.False(actual);
		}
	}
}
