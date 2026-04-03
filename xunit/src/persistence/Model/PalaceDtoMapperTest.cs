namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Civilizations;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class PalaceDtoMapperTest : TestsBase2
	{
		private readonly PalaceDtoMapper _testee;
		private readonly PalaceData _palaceData;

		public PalaceDtoMapperTest()
		{
			_testee = new PalaceDtoMapper();
			_palaceData = new PalaceData();
			_palaceData.SetGarden(0, 0);
			_palaceData.SetGarden(1, 1);
			_palaceData.SetGarden(2, 2);
			_palaceData.SetPalace(0, 0, 0);
			_palaceData.SetPalace(1, 1, 1);
			_palaceData.SetPalace(2, 2, 2);
			_palaceData.SetPalace(3, 0, 0);
			_palaceData.SetPalace(4, 1, 1);
			_palaceData.SetPalace(5, 2, 2);
			_palaceData.SetPalace(6, 0, 0);
		}

		[Fact]
		public void TestPalaceDtoMapper_ContractCheck()
		{
			var dto = _testee.ToDto(_palaceData);
			var dtoProperties = GetWritablePropertyNames<PalaceDto>();
			var expectedProperties = GetPalaceDtoRoundTripAssertionMap(dto, dto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestMapPalaceDataToDtoAndBack()
		{
			var dto = _testee.ToDto(_palaceData);
			var palace2 = _testee.FromDto(dto);

			for (int i = 0; i < 7; i++)
			{
				Assert.Equal(_palaceData.GetPalaceStyle(i), palace2.GetPalaceStyle(i));
				Assert.Equal(_palaceData.GetPalaceLevel(i), palace2.GetPalaceLevel(i));
			}
			for (int i = 0; i < 3; i++)
			{
				Assert.Equal(_palaceData.GetGardenLevel(i), palace2.GetGardenLevel(i));
			}
		}

		[Fact]
		public void TestToDto()
		{
			var dto = _testee.ToDto(_palaceData);
			Assert.NotNull(dto);

			YamlWriter.Of(dto)
				.WithStandard()
				.ToFile("palaceDto.yaml");
		}

		[Fact]
		public void TestPalaceDtoMapper_RoundTrip()
		{
			var expected = _testee.ToDto(_palaceData);
			var roundTripDto = _testee.ToDto(_testee.FromDto(expected));

			Assert.NotNull(roundTripDto);

			var assertions = GetPalaceDtoRoundTripAssertionMap(expected, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetPalaceDtoRoundTripAssertionMap(PalaceDto expected, PalaceDto actual)
			=> new()
			{
				[nameof(PalaceDto.LeftTower)] = () => AssertPalaceSectionEqual(expected.LeftTower, actual.LeftTower),
				[nameof(PalaceDto.LeftWing)] = () => AssertPalaceSectionEqual(expected.LeftWing, actual.LeftWing),
				[nameof(PalaceDto.LeftAnnex)] = () => AssertPalaceSectionEqual(expected.LeftAnnex, actual.LeftAnnex),
				[nameof(PalaceDto.Center)] = () => AssertPalaceSectionEqual(expected.Center, actual.Center),
				[nameof(PalaceDto.RightAnnex)] = () => AssertPalaceSectionEqual(expected.RightAnnex, actual.RightAnnex),
				[nameof(PalaceDto.RightWing)] = () => AssertPalaceSectionEqual(expected.RightWing, actual.RightWing),
				[nameof(PalaceDto.RightTower)] = () => AssertPalaceSectionEqual(expected.RightTower, actual.RightTower),
				[nameof(PalaceDto.GardenLeftLevel)] = () => Assert.Equal(expected.GardenLeftLevel, actual.GardenLeftLevel),
				[nameof(PalaceDto.GardenCenterLevel)] = () => Assert.Equal(expected.GardenCenterLevel, actual.GardenCenterLevel),
				[nameof(PalaceDto.GardenRightLevel)] = () => Assert.Equal(expected.GardenRightLevel, actual.GardenRightLevel)
			};

		private static void AssertPalaceSectionEqual(PalaceSectionDto expected, PalaceSectionDto actual)
		{
			Assert.NotNull(actual);
			Assert.Equal(expected.Style, actual.Style);
			Assert.Equal(expected.Level, actual.Level);
		}

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)
			.ToHashSet();
	}
}