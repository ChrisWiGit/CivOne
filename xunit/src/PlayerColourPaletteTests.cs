using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CivOne.UnitTests
{
	public class PlayerColourPaletteTests
	{
		[Fact]
		public void PaletteTablesHaveOneEntryPerMaxPlayer()
		{
			Assert.Equal(Game.MaxPlayers, Common.ColourLight.Length);
			Assert.Equal(Game.MaxPlayers, Common.ColourDark.Length);
		}

		[Fact]
		public void FirstEightEntriesMatchTheOriginalEightPlayerPalette()
		{
			byte[] originalLight = [12, 15, 10, 9, 14, 11, 13, 7];
			byte[] originalDark = [4, 7, 2, 1, 10, 3, 4, 8];

			Assert.Equal(originalLight, Common.ColourLight.Take(8));
			Assert.Equal(originalDark, Common.ColourDark.Take(8));
		}

		[Fact]
		public void EveryLightDarkPairIsUniqueAndDistinguishable()
		{
			var pairs = new HashSet<(byte Light, byte Dark)>();

			for (int i = 0; i < Game.MaxPlayers; i++)
			{
				byte light = Common.ColourLight[i];
				byte dark = Common.ColourDark[i];

				Assert.NotEqual(0, dark);
				Assert.NotEqual(light, dark);
				Assert.True(pairs.Add((light, dark)), $"Duplicate (light, dark) colour pair at player index {i}.");
			}
		}

		[Fact]
		public void PlayerColourHelpersWrapOutOfRangeIndices()
		{
			Assert.Equal(Common.ColourLight[0], Common.PlayerColourLight(Game.MaxPlayers));
			Assert.Equal(Common.ColourDark[0], Common.PlayerColourDark(Game.MaxPlayers));
			Assert.Equal(Common.ColourLight[3], Common.PlayerColourLight(3));
		}
	}
}
