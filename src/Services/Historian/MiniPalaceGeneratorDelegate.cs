using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

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
	/// Deliberate deviation: CivOneX palaces have 7 sections, which are the original sections 1..7.
	/// The generator still runs on 9 sections, because 0 and 8 influence the others, but those two edge pieces
	/// are not shown.
	/// The original counts levels from -1 (no section) and 0 (base built) to 3; CivOneX counts from 0 (empty) to 4.
	/// Each level is therefore shifted by one. Shifted levels above 4 (only reachable through the forced writes) are capped at 4.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, Phase 5.
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
		/// <returns>A palace with the generated sections; gardens stay empty.</returns>
		public PalaceData Generate(int playerIndex, int buildCount, int randomSeed)
		{
			int[] levels = GenerateOriginalLevels(playerIndex, buildCount, randomSeed);
			byte style = (byte)((PalaceStyle)((playerIndex % 3) + 1));

			PalaceData palace = new();
			for (int k = 0; k < 7; k++)
			{
				byte level = (byte)Math.Clamp(levels[k + 1] + 1, 0, 4);
				palace.SetPalace(k, style, level);
			}

			return palace;
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
