using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;

namespace CivOne.Services.Historian
{
	/// <summary>
	/// Generates the Historian mini palace of an AI player like the original game.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The original places <c>clamp(buildCount + 3, 3, 36)</c> pieces on 9 sections, chosen by a 16-bit
	/// pseudo-random sequence seeded with <c>playerIndex * randomSeed</c>; the section is the low byte modulo 9.
	/// After 999 rejected tries the last candidate is written anyway. The asymmetric mirror check
	/// (<c>7 - s</c> left, <c>9 - s</c> right) is an original quirk and kept, so the result stays identical.
	/// </para>
	/// <para>
	/// The levels are returned unchanged on the original's scale: -1 is no section, 0 a base section that is not
	/// drawn, 1 and up are drawn. Levels above 4, only reachable through the forced writes, are capped at 4.
	/// All 9 sections are kept; the mini palace has a slot for each.
	/// See <c>docs/plan-palace.impl.md</c>, 5b.1.
	/// </para>
	/// </remarks>
	internal sealed class MiniPalaceGeneratorDelegate
	{
		private const int OriginalSections = 9;
		private const int MaxTries = 999;

		/// <summary>
		/// Generates the mini palace.
		/// </summary>
		/// <param name="playerIndex">The player slot, 0..7.</param>
		/// <param name="buildCount">The build count from <see cref="IHistorianPalaceService.PalaceLevel"/>.</param>
		/// <param name="randomSeed">The game's random seed (the map's terrain master word).</param>
		/// <returns>The mini palace; every slot has the player's style.</returns>
		public MiniPalaceLayout Generate(int playerIndex, int buildCount, int randomSeed)
		{
			int[] levels = GenerateOriginalLevels(playerIndex, buildCount, randomSeed);
			PalaceStyle style = (PalaceStyle)((playerIndex % 3) + 1);
			PalaceStyle[] styles = [.. Enumerable.Repeat(style, MiniPalaceLayout.SlotCount)];

			return new(levels, styles);
		}

		/// <summary>
		/// Runs the original generator on its 9 sections.
		/// </summary>
		/// <param name="playerIndex">The player slot, 0..7.</param>
		/// <param name="buildCount">The build count.</param>
		/// <param name="randomSeed">The game's random seed.</param>
		/// <returns>The 9 original section levels; -1 means no section.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public int[] GenerateOriginalLevels(int playerIndex, int buildCount, int randomSeed)
		{
			int pieces = Math.Clamp(buildCount + 3, 3, 36);
			int threshold = pieces / 5;
			int[] levels = [-1, -1, -1, 0, 0, 0, -1, -1, -1];
			ushort random = unchecked((ushort)(playerIndex * randomSeed));

			for (int i = 0; i < pieces; i++)
			{
				int section = 0;
				int level = 0;
				for (int attempt = 0; attempt < MaxTries; attempt++)
				{
					random = unchecked((ushort)((random * 5) + 1));
					section = (random & 0xFF) % OriginalSections;
					level = levels[section] + 1;
					if (IsAccepted(levels, section, level, threshold))
					{
						break;
					}
				}

				levels[section] = level;
			}

			return levels;
		}

		private static bool IsAccepted(int[] levels, int section, int level, int threshold)
		{
			if (level > threshold || level > 3)
			{
				return false;
			}
			if (section < 4 && (levels[section + 1] < level || levels[7 - section] == -1))
			{
				return false;
			}
			if (section > 4 && (levels[section - 1] < level || levels[9 - section] == -1))
			{
				return false;
			}

			return true;
		}
	}
}
