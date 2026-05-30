using CivOne.Enums;
using Xunit;

namespace CivOne.UnitTests
{
	public class MapRiverFallbackChanceTests
	{
		[Theory]
		[InlineData(Climate.Arid, 0)]
		[InlineData(Climate.Normal, 25)]
		[InlineData(Climate.Wet, 50)]
		public void ClimateMapsToRiverFallbackChance(Climate climate, int expectedChance)
		{
			Assert.Equal(expectedChance, Map.ComputeClimateRiverFallbackChancePercent(climate));
		}

		[Theory]
		[InlineData(Climate.Arid, 1024)]
		[InlineData(Climate.Normal, 2048)]
		[InlineData(Climate.Wet, 4096)]
		public void ClimateMapsToRiverStartSearchMinimumAttempts(Climate climate, int expectedAttempts)
		{
			Assert.Equal(expectedAttempts, Map.ComputeClimateRiverStartSearchMinimumAttempts(climate));
		}

		[Theory]
		[InlineData(Climate.Arid, 3)]
		[InlineData(Climate.Normal, 4)]
		[InlineData(Climate.Wet, 5)]
		public void ClimateMapsToRiverStartSearchFactor(Climate climate, int expectedFactor)
		{
			Assert.Equal(expectedFactor, Map.ComputeClimateRiverStartSearchFactor(climate));
		}

		[Theory]
		[InlineData(EarthAge.ThreeBillionYears, 50)]
		[InlineData(EarthAge.FourBillionYears, 70)]
		[InlineData(EarthAge.FiveBillionYears, 85)]
		public void AgeMapsToMountainGenerationReductionPercent(EarthAge age, int expectedReduction)
		{
			Assert.Equal(expectedReduction, Map.ComputeMountainGenerationReductionPercent(age));
		}
	}
}