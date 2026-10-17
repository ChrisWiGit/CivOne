using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Services.Palace;
using Xunit;

namespace CivOne.UnitTests.Services.Palace
{
	public class SvePalaceDelegateTests
	{
		private static readonly short[] StartPalaceLevels = [-1, -1, -1, 0, 0, 0, -1, -1, -1, 0, 0, 0];
		private static readonly short[] ShiftedLevels = [-1, 2, -1, 0, 4, 0, -1, -1, -1, 0, 3, 0];

		private readonly SvePalaceDelegate _testee = new();

		[Fact]
		public void StartPalaceIsWrittenLikeTheOriginal()
		{
			short[] block = _testee.ToSve(new PalaceData());

			Assert.Equal(StartPalaceLevels, block[..12]);
			Assert.All(block[12..], style => Assert.Equal(0, style));
		}

		[Fact]
		public void SectionsAreShiftedByOneAndStylesStartAtZero()
		{
			PalaceData palace = new();
			palace.SetPalace(0, (byte)PalaceStyle.Islamic, 2);
			palace.SetPalace(3, (byte)PalaceStyle.Classical, 4);
			palace.SetGarden(1, 3);

			short[] block = _testee.ToSve(palace);

			Assert.Equal(ShiftedLevels, block[..12]);
			Assert.Equal(2, block[12 + 1]);
			Assert.Equal(1, block[12 + 4]);
		}

		[Fact]
		public void MixedPalaceRoundTrips()
		{
			PalaceData expected = new();
			expected.SetPalace(0, (byte)PalaceStyle.Medieval, 1);
			expected.SetPalace(1, (byte)PalaceStyle.Medieval, 4);
			expected.SetPalace(2, (byte)PalaceStyle.Classical, 3);
			expected.SetPalace(4, (byte)PalaceStyle.Islamic, 2);
			expected.SetGarden(0, 1);
			expected.SetGarden(2, 3);

			PalaceData actual = _testee.FromSve(_testee.ToSve(expected), 13);

			for (int k = 0; k < 7; k++)
			{
				Assert.Equal(expected.GetPalaceLevel(k), actual.GetPalaceLevel(k));
				Assert.Equal(expected.GetPalaceStyle(k), actual.GetPalaceStyle(k));
			}
			for (int g = 0; g < 3; g++)
			{
				Assert.Equal(expected.GetGardenLevel(g), actual.GetGardenLevel(g));
			}
			Assert.Equal(13, actual.BuildCount);
		}

		[Fact]
		public void InvalidValuesAreCorrectedAndLogged()
		{
			RecordingLogger logger = new();
			SvePalaceDelegate testee = new(logger);
			short[] block = new short[24];
			short[] levels = [3, -1, 7, -1, 2, 0, -1, -1, 5, 9, 0, 0];
			levels.CopyTo(block, 0);
			block[12 + 2] = 5;

			PalaceData actual = testee.FromSve(block, 0);

			Assert.Equal(4, actual.GetPalaceLevel(1));
			Assert.Equal(PalaceStyle.Medieval, actual.GetPalaceStyle(1));
			Assert.Equal(0, actual.GetPalaceLevel(2));
			Assert.Equal(3, actual.GetGardenLevel(0));
			Assert.NotEmpty(logger.Messages);
		}

		[Fact]
		public void ZeroFilledBlockFromOlderSavesGivesTheStartPalace()
		{
			PalaceData actual = _testee.FromSve(new short[24], 0);

			Assert.Equal(0, actual.UpgradeCount);
		}

		private sealed class RecordingLogger : ILogger
		{
			public List<string> Messages { get; } = [];

			public void Log(string text, params object[] parameters) => Messages.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, text, parameters));
		}
	}
}
