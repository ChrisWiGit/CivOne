namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Linq;
	using CivOne.Advances;
	using CivOne.Civilizations;
	using CivOne.Leaders;
	using CivOne.Enums;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class PlayerDtoMapperTest : TestsBase2
	{
		private readonly PlayerDto originalDto;
		private readonly PlayerDtoMapper _testee;
		private readonly MockedIPlayer _player;
		private readonly MockedICity _city;
		private readonly MockedIUnit _unit;

		public PlayerDtoMapperTest()
		{
			var civsInGame = MockedICivilization.Mock(3);
			// how to initialize in real game: var civs = Common.Civilizations.Select(c => c.Leader.GetType().Name);
			// * var civsInGame = Common.Civilizations;
			// * var govs = Reflect.GetGovernments();
			// * PlayerDto.AllGovernments = govs.Select(g => $"{g.GetType().Name}:{g.Id}").ToArray();
			string[] classes = [.. this.GetType().Assembly.GetTypes()
				.Where(t => typeof(ILeader).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				.Select(t => t.Name)];
			
			CivilizationDto.AllLeaderClassNames = classes;

			_city = new MockedICity(1)
			{
				Name = "Rome",
				Owner = 0,
				Size = 7
			};

			_unit = new MockedIUnit
			{
				Owner = 0,
				X = 10,
				Y = 20,
				Goto = new Point(5, 8),
				Veteran = true,
				MovesLeft = 2,
				PartMoves = 1,
				order = Order.Fortify
			};

			_player = new MockedIPlayer()
			{
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = new MockedIAdvance() { Id = 1 },
				Government = new MockedIGovernment() { Id = 1 },
				Palace = new MockedIPalace(),
				Cities = [_city],
			};

			originalDto = new PlayerDto
			{
				Id = 0,
				Civilization = new CivilizationDto { LeaderClassName = new MockedILeader().GetType().Name },
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = 1,
				Government = 1,
				Palace = new PalaceDto(),
				Cities = [
					new CityDto
					{
						Name = "Rome",
						Owner = 0,
						Size = 7,
						Location = new MapLocation(10, 10),
						VisibleSizes = [7],
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						TradingCities = [],
						ContinentId = 1
					}
				],
				Units = [
					new UnitDto
					{
						ClassName = "MockedIUnit",
						PlayerId = 0,
						Location = new MapLocation(10, 20),
						Goto = new MapLocation(5, 8),
						Veteran = true,
						MovesLeft = 2,
						PartMoves = 1,
						Order = Order.Fortify
					}
				],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5),
				TribeName = "Romans",
				TribeNamePlural = "Romans",
				LuxuriesRate = 0,
				TaxesRate = 5,
				ScienceRate = 5,
				Science = 100,
				CityNamesSkipped = 0
			};

			// Setup game instance mock
			var gameInstance = new MockPlayerGameForTesting(_player, [_unit]);
			
			_testee = new PlayerDtoMapper(
				gameInstance,
				new FixedPlayerOwnerResolver(0),
				new MockPlayerFactoryForTesting(_player),
				new CivilizationMapper(civsInGame),
				new PalaceDtoMapper(),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect())),
				new UnitDtoMapper(new MockUnitFactoryForTesting()));
			
			PlayerDto.AllAdvances = ["0(Advance0)", "1(Advance1)", "2(Advance2)", "3(Advance3)"];
			PlayerDto.AllAdvancesInfo = new Dictionary<AdvanceId, string>
							{   { 0, "Advance0" },
								{ 1, "Advance1" },
								{ 2, "Advance2" },
								{ 3, "Advance3" }

							};
			PlayerDto.AllGovernments = ["0(Government0)", "1(Government1)", "42(MockedGovernment42)"];
		}

		[Fact]
		public void TestPlayerDtoMapper_ContractCheck()
		{
			// Verify that PlayerDto has all required properties for serialization
			var requiredProperties = new[]
			{
				nameof(PlayerDto.TribeName),
				nameof(PlayerDto.TribeNamePlural),
				nameof(PlayerDto.Anarchy),
				nameof(PlayerDto.Gold),
				nameof(PlayerDto.CurrentResearch),
				nameof(PlayerDto.Government),
				nameof(PlayerDto.LuxuriesRate),
				nameof(PlayerDto.TaxesRate),
				nameof(PlayerDto.ScienceRate),
				nameof(PlayerDto.Science),
				nameof(PlayerDto.CityNamesSkipped),
				nameof(PlayerDto.Advances),
				nameof(PlayerDto.Embassies),
				nameof(PlayerDto.Explored),
				nameof(PlayerDto.Visible),
				nameof(PlayerDto.Civilization),
				nameof(PlayerDto.Palace),
				nameof(PlayerDto.Cities),
				nameof(PlayerDto.Units)
			};

			var dtoProperties = typeof(PlayerDto).GetProperties()
				.Where(p => p.CanRead && p.CanWrite)
				.Select(p => p.Name)
				.ToHashSet();

			foreach (var prop in requiredProperties)
			{
				Assert.Contains(prop, dtoProperties);
			}
		}

		[Fact]
		public void TestPlayerDtoMapper_RoundTrip()
		{
			// Act: Convert PlayerDto -> IPlayer -> PlayerDto
			var player = _testee.FromDto(originalDto);
			var roundTripDto = _testee.ToDto(player);

			YamlWriter.Of(roundTripDto).WithStandard().ToFile("PlayerDtoMapperTest.yaml");

			// Assert: Round-trip consistency - all properties should be preserved
			Assert.NotNull(roundTripDto);
			Assert.Equal(originalDto.TribeName, roundTripDto.TribeName);
			Assert.Equal(originalDto.TribeNamePlural, roundTripDto.TribeNamePlural);
			Assert.Equal(originalDto.Anarchy, roundTripDto.Anarchy);
			Assert.Equal(originalDto.Gold, roundTripDto.Gold);
			Assert.Equal(originalDto.CurrentResearch, roundTripDto.CurrentResearch);
			Assert.Equal(originalDto.Government, roundTripDto.Government);
			Assert.Equal(originalDto.LuxuriesRate, roundTripDto.LuxuriesRate);
			Assert.Equal(originalDto.TaxesRate, roundTripDto.TaxesRate);
			Assert.Equal(originalDto.ScienceRate, roundTripDto.ScienceRate); // Now properly preserved
			Assert.Equal(originalDto.Science, roundTripDto.Science);
			Assert.Equal(originalDto.CityNamesSkipped, roundTripDto.CityNamesSkipped);
			Assert.Equal(originalDto.Advances.Count, roundTripDto.Advances.Count);
			Assert.Equal(originalDto.Embassies.Count, roundTripDto.Embassies.Count);

			Assert.Single(roundTripDto.Cities);
			Assert.Equal("Rome", roundTripDto.Cities[0].Name);
			Assert.Equal((uint)7, roundTripDto.Cities[0].Size);
			Assert.Equal((byte)0, roundTripDto.Cities[0].Owner);

			Assert.Single(roundTripDto.Units);
			Assert.Equal((uint)10, roundTripDto.Units[0].Location.X);
			Assert.Equal((uint)20, roundTripDto.Units[0].Location.Y);
			Assert.True(roundTripDto.Units[0].Veteran);
			Assert.Equal(2, roundTripDto.Units[0].MovesLeft);
			Assert.Equal(1, roundTripDto.Units[0].PartMoves);
			Assert.Equal(Order.Fortify, roundTripDto.Units[0].Order);
		}

		private class MockPlayerGameForTesting : IPlayerGame
		{
			private readonly IPlayer _player;
			private readonly IUnit[] _units;

			public MockPlayerGameForTesting(IPlayer player, IUnit[] units = null)
			{
				_player = player;
				_units = units ?? [];
			}

			public bool Started => true;
			public ushort GameTurn => 0;
			public int Difficulty => 3;
			public Player HumanPlayer => throw new NotImplementedException();
			public Player CurrentPlayer => throw new NotImplementedException();
			public IEnumerable<Player> Players => [(_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance")];

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
			public City[] GetCities() => [];
			public IUnit[] GetUnits() => _units;
			public void DisbandUnit(IUnit unit) => throw new NotImplementedException();
			public bool WonderObsolete<T>() where T : IWonder, new() => false;
			public bool WonderBuilt<T>() where T : IWonder => false;
			public IWonder[] BuiltWonders => [];
			public void SetAdvanceOrigin(IAdvance advance, Player player) => throw new NotImplementedException();
		}

		private class MockPlayerFactoryForTesting : IPlayerFactory
		{
			private readonly MockedIPlayer _mockPlayer;

			public MockPlayerFactoryForTesting(MockedIPlayer mockPlayer)
			{
				_mockPlayer = mockPlayer ?? throw new ArgumentNullException(nameof(mockPlayer));
			}

			public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
			{
				// Return the mock player which already implements IPlayerRestorable
				return _mockPlayer;
			}
		}

		private class MockUnitDtoMapperForTesting : DtoMapper<UnitDto, IUnit>
		{
			public IUnit FromDto(UnitDto dto) => throw new NotImplementedException();
			public UnitDto ToDto(IUnit domain) => throw new NotImplementedException();
		}

		private class MockUnitFactoryForTesting : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
				=> throw new NotImplementedException();
		}

		private class FixedPlayerOwnerResolver : IPlayerOwnerResolver
		{
			private readonly byte _ownerId;

			public FixedPlayerOwnerResolver(byte ownerId)
			{
				_ownerId = ownerId;
			}

			public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
			{
				ownerId = _ownerId;
				return true;
			}
		}
	}
}

/*
		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterSingleRow()
		{
			byte[][] testData = [[1, 2, 3]];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.AsString();

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();

			Assert.Single(roundTripped);
			Assert.Equal(new byte[] { 1, 2, 3 }, roundTripped[0]);
		}

*/