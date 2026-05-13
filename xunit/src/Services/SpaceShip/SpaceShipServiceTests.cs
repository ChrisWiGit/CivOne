using Xunit;
using CivOne.Services.SpaceShip;
using CivOne.Enums;
using System;

namespace CivOne.UnitTests.Services.SpaceShip
{
	public class SpaceShipServiceTests
	{
		[Fact]
		public void CanAddAnyPart_WithNoPartsAllowed_ReturnsFalse()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(_ => false, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanAddAnyPart();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CanAddAnyPart_WithApolloAndTech_ReturnsTrue()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(partType => partType == SpaceShipComponentType.Structural, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanAddAnyPart();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void CanAddPart_Structural_WithApolloAndSpaceFlight_ReturnsTrue()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(partType => partType == SpaceShipComponentType.Structural, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanAddPart(SpaceShipComponentType.Structural);

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void CanAddPart_Component_WithoutPlastics_ReturnsFalse()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(partType => partType == SpaceShipComponentType.Structural, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanAddPart(SpaceShipComponentType.Component);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryAddPart_Structural_SuccessfullyAddsToGrid()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(_ => true, (_, _) => true);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch) with { Population = 10_000 });
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.TryAddPart(SpaceShipComponentType.Structural);

			// Assert
			Assert.True(result);
			Assert.NotEqual(0, player.SpaceShipPopulation);
		}

		[Fact]
		public void TryAddPart_Full_GridDoesNotAcceptMoreParts()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(_ => true, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.TryAddPart(SpaceShipComponentType.Structural);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CanLaunch_WithInsufficientParts_ReturnsFalse()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(_ => false, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanLaunch();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CanLaunch_WithSufficientParts_ReturnsTrue()
		{
			// Arrange
			var player = CreatePlayer();
			var placementRules = new StubPlacementRules(_ => false, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => true);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanLaunch();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void CanLaunch_AfterLaunched_ReturnsFalse()
		{
			// Arrange
			var player = CreatePlayer();
			player.SpaceShipLaunchYear = 2100;
			var placementRules = new StubPlacementRules(_ => false, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => false);
			var screenDataFactory = new StubScreenDataFactory((_, canLaunch) => EmptyData(canLaunch));
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var result = service.CanLaunch();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void GetScreenData_ReturnsValidData()
		{
			// Arrange
			var player = CreatePlayer();
			var expected = new SpaceShipScreenData(40_000, 100, 100, 24_700, 100, 15.2, 95, 4, 2, 1, 7, true);
			var placementRules = new StubPlacementRules(_ => false, (_, _) => false);
			var launchRules = new StubLaunchRules(_ => true);
			var screenDataFactory = new StubScreenDataFactory((_, _) => expected);
			var service = new SpaceShipService(player, placementRules, launchRules, screenDataFactory);

			// Act
			var data = service.GetScreenData();

			// Assert
			Assert.NotNull(data);
			Assert.Equal(expected.Population, data.Population);
			Assert.Equal(expected.StructuralCount, data.StructuralCount);
			Assert.Equal(expected.ComponentCount, data.ComponentCount);
			Assert.Equal(expected.ModuleCount, data.ModuleCount);
			Assert.Equal(expected.SuccessProbabilityPercent, data.SuccessProbabilityPercent);
		}

		private static IPlayerSpaceRace CreatePlayer()
		{
			return new FakePlayerSpaceRace
			{
				SpaceShipGrid = new SpaceShipComponentType[12, 12],
				SpaceShipPopulation = 0,
				SpaceShipLaunchYear = 0
			};
		}

		private static SpaceShipScreenData EmptyData(bool canLaunch)
		{
			return new SpaceShipScreenData(0, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0, canLaunch);
		}

		private sealed class StubPlacementRules(
			Func<SpaceShipComponentType, bool> canAddPart,
			Func<IPlayerSpaceRace, SpaceShipComponentType, bool> tryAddPart) : ISpaceShipPlacementRules
		{
			private readonly Func<SpaceShipComponentType, bool> _canAddPart = canAddPart;
			private readonly Func<IPlayerSpaceRace, SpaceShipComponentType, bool> _tryAddPart = tryAddPart;

			public bool CanAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType) => _canAddPart(partType);

			public bool TryAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType) => _tryAddPart(player, partType);
		}

		private sealed class StubLaunchRules(Func<IPlayerSpaceRace, bool> canLaunch) : ISpaceShipLaunchRules
		{
			private readonly Func<IPlayerSpaceRace, bool> _canLaunch = canLaunch;

			public bool CanLaunch(IPlayerSpaceRace player) => _canLaunch(player);
		}

		private sealed class StubScreenDataFactory(Func<IPlayerSpaceRace, bool, SpaceShipScreenData> create) : ISpaceShipScreenDataFactory
		{
			private readonly Func<IPlayerSpaceRace, bool, SpaceShipScreenData> _create = create;

			public SpaceShipScreenData Create(IPlayerSpaceRace player, bool canLaunch) => _create(player, canLaunch);
		}

		private sealed class FakePlayerSpaceRace : IPlayerSpaceRace
		{
			public SpaceShipComponentType[,] SpaceShipGrid { get; set; }

			public ushort SpaceShipPopulation { get; set; }

			public short SpaceShipLaunchYear { get; set; }

			public bool HasSpaceFlightAdvance() => false;

			public bool HasPlasticsAdvance() => false;

			public bool HasRoboticsAdvance() => false;

			public bool HasApolloProgram() => false;
		}
	}
}
