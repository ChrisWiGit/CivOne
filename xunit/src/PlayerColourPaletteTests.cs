using System.Collections.Generic;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the player colour palette for all <see cref="Game.MaxPlayers"/> slots.
	/// The tables themselves are private, so every assertion goes through the same accessors the game uses.
	/// </summary>
	public class PlayerColourPaletteTests
	{
		[Fact]
		public void PaletteTablesHaveOneEntryPerMaxPlayer()
		{
			Assert.Equal(Game.MaxPlayers, Common.PlayerColourCount);
		}

		[Fact]
		public void FirstEightEntriesMatchTheOriginalEightPlayerPalette()
		{
			byte[] originalLight = [12, 15, 10, 9, 14, 11, 13, 7];
			byte[] originalDark = [4, 7, 2, 1, 10, 3, 4, 8];

			for (int i = 0; i < originalLight.Length; i++)
			{
				Assert.Equal(originalLight[i], Common.PlayerColourLight(i));
				Assert.Equal(originalDark[i], Common.PlayerColourDark(i));
			}
		}

		[Fact]
		public void EveryLightDarkPairIsUniqueAndDistinguishable()
		{
			var pairs = new HashSet<(byte Light, byte Dark)>();

			for (int i = 0; i < Game.MaxPlayers; i++)
			{
				byte light = Common.PlayerColourLight(i);
				byte dark = Common.PlayerColourDark(i);

				Assert.NotEqual(0, dark);
				Assert.NotEqual(light, dark);
				Assert.True(pairs.Add((light, dark)), $"Duplicate (light, dark) colour pair at player index {i}.");
			}
		}

		[Fact]
		public void PlayerColourHelpersWrapOutOfRangeIndices()
		{
			Assert.Equal(Common.PlayerColourLight(0), Common.PlayerColourLight(Game.MaxPlayers));
			Assert.Equal(Common.PlayerColourDark(0), Common.PlayerColourDark(Game.MaxPlayers));

			// Negative indices must wrap too, so a stray -1 cannot throw in a drawing path.
			Assert.Equal(Common.PlayerColourLight(Game.MaxPlayers - 1), Common.PlayerColourLight(-1));
			Assert.Equal(Common.PlayerColourDark(Game.MaxPlayers - 1), Common.PlayerColourDark(-1));
		}
	}
}
