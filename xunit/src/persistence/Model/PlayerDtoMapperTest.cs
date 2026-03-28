namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Civilizations;
	using CivOne.Leaders;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
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
			string[] classes = this.GetType().Assembly.GetTypes()
				.Where(t => typeof(ILeader).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				.Select(t => t.Name)
				.ToArray();
			
			CivilizationDto.AllLeaderClassNames = classes;

			_testee = new PlayerDtoMapper(
				null, // IPlayerGame – only used in FromDto, not needed for ToDto
				new CivilizationMapper(civsInGame),
				new PalaceDtoMapper(),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect())));
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
	}
}
