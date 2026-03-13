namespace CivOne.Persistence.Model
{
	using System;
	using System.Linq;
	using CivOne.Civilizations;
	using CivOne.Persistence.YamlConverter;
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
	}
}