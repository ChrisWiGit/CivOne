using System.Linq;
using CivOne.Enums;
using CivOne.Services.Historian;
using Xunit;

namespace CivOne.UnitTests.Services.Historian
{
	public class MiniPalaceGeneratorDelegateTests
	{
		// Reference values from a simulation of the DOS routine with 16-bit semantics.
		private static readonly int[] NoForcedWrites = [0, 0, 0, 2, 2, 2, 1, 1, -1];
		private static readonly int[] WithForcedWrites = [3, 3, 4, 3, 3, 3, 4, 3, 4];
		private static readonly byte[] SectionsOneToSeven = [1, 1, 3, 3, 3, 2, 2];

		private readonly MiniPalaceGeneratorDelegate _generator = new();

		[Theory]
		[InlineData(0, 0)]
		[InlineData(5, 0)]
		[InlineData(6, 1)]
		[InlineData(15, 1)]
		[InlineData(16, 2)]
		[InlineData(50, 3)]
		[InlineData(150, 7)]
		[InlineData(-20, 0)]
		public void PalaceLevelFollowsOriginalLoop(int citySum, int expected)
		{
			Assert.Equal(expected, new HistorianPalaceService().PalaceLevel(citySum));
		}

		[Fact]
		public void GeneratorMatchesDosReferenceWithoutForcedWrites()
		{
			Assert.Equal(NoForcedWrites, _generator.GenerateOriginalLevels(1, 10, 1234));
		}

		[Fact]
		public void GeneratorMatchesDosReferenceWithForcedWrites()
		{
			Assert.Equal(WithForcedWrites, _generator.GenerateOriginalLevels(2, 33, 1234));
		}

		[Fact]
		public void GeneratorIsDeterministic()
		{
			Assert.Equal(_generator.GenerateOriginalLevels(5, 20, 4711), _generator.GenerateOriginalLevels(5, 20, 4711));
		}

		[Fact]
		public void PalaceUsesOriginalSectionsOneToSevenAndPlayerStyle()
		{
			PalaceData palace = _generator.Generate(1, 10, 1234);

			byte[] levels = [.. Enumerable.Range(0, 7).Select(palace.GetPalaceLevel)];
			Assert.Equal(SectionsOneToSeven, levels);
			Assert.Equal(PalaceStyle.Classical, palace.GetPalaceStyle(2));
			Assert.Equal(PalaceStyle.Classical, palace.GetPalaceStyle(0));
		}

		[Fact]
		public void SmallestPalaceShowsTheBaseSections()
		{
			PalaceData palace = _generator.Generate(1, 0, 1234);

			Assert.True(palace.GetPalaceLevel(2) > 0);
			Assert.True(palace.GetPalaceLevel(3) > 0);
			Assert.True(palace.GetPalaceLevel(4) > 0);
		}

		[Fact]
		public void ForcedLevelsAreCappedAtFour()
		{
			PalaceData palace = _generator.Generate(2, 33, 1234);

			for (int i = 0; i < 7; i++)
			{
				Assert.InRange(palace.GetPalaceLevel(i), (byte)0, (byte)4);
			}
		}
	}
}
