namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Advances;
	using CivOne.Civilizations;
	using CivOne.Leaders;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class PlayerDtoMapperTest
	{
		private readonly PlayerDtoMapper _testee;
		private readonly MockedIPlayer _player;

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

			_player = new MockedIPlayer()
			{
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = new MockedIAdvance() { Id = 1 },
				Government = new MockedIGovernment() { Id = 1 },
				Palace = new MockedIPalace(),
			};

			// Setup game instance mock
			var gameInstance = new MockPlayerGameForTesting(_player);
			
			_testee = new PlayerDtoMapper(
				gameInstance,
				new MockPlayerFactoryForTesting(),
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
		public void TestPlayerDtoMapper()
		{
			var dto = _testee.ToDto(_player);
			Assert.NotNull(dto);

			YamlWriter.Of(dto).WithStandard().ToFile("PlayerDtoMapperTest.TestPlayerDtoMapper.yaml");
		}

		// Mock implementations for testing
		private class MockPlayerGameForTesting : IPlayerGame
		{
			private readonly IPlayer _player;
			public MockPlayerGameForTesting(IPlayer player) => _player = player;

			public bool Started => true;
			public ushort GameTurn => 0;
			public int Difficulty => 3;
			public Player HumanPlayer => throw new NotImplementedException();
			public Player CurrentPlayer => throw new NotImplementedException();
			public IEnumerable<Player> Players => [(_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance")];

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
			public City[] GetCities() => [];
			public IUnit[] GetUnits() => []; // No units for this test
			public void DisbandUnit(IUnit unit) => throw new NotImplementedException();
			public bool WonderObsolete<T>() where T : IWonder, new() => false;
			public bool WonderBuilt<T>() where T : IWonder => false;
			public IWonder[] BuiltWonders => [];
			public void SetAdvanceOrigin(IAdvance advance, Player player) => throw new NotImplementedException();
		}

		private class MockPlayerFactoryForTesting : IPlayerFactory
		{
			public IPlayer Create(ICivilization civilization, PlayerDto dto) => throw new NotImplementedException();
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