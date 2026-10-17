using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.Services.Diplomacy
{
	/// <summary>
	/// Converts between CivOneX's per-player diplomacy state and the diplomacy block of an SVE file.
	/// </summary>
	/// <remarks>
	/// The SVE block holds 8 rows of 8 flag words: row = player, column = the other player.
	/// Like the original, bit <see cref="EmbassyFlag"/> marks an established embassy.
	/// CivOneX keeps embassies in their own list, so on save only that bit is written;
	/// the other flags are not maintained by CivOneX and would be stale.
	/// </remarks>
	internal sealed class SveDiplomacyDelegate
	{
		/// <summary>
		/// The number of player slots in an SVE file.
		/// </summary>
		public const int SlotCount = 8;

		/// <summary>
		/// The diplomacy flag for an established embassy.
		/// </summary>
		public const ushort EmbassyFlag = 0x40;

		/// <summary>
		/// Reads one player's row from the SVE diplomacy block.
		/// </summary>
		/// <param name="block">The 64 flag words of the SVE file.</param>
		/// <param name="player">The player slot, 0..7.</param>
		/// <returns>The 8 flag words of <paramref name="player"/>; missing values are 0.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public ushort[] RowOf(IReadOnlyList<ushort> block, int player)
		{
			ArgumentNullException.ThrowIfNull(block);

			ushort[] row = new ushort[SlotCount];
			for (int target = 0; target < SlotCount; target++)
			{
				int index = (player * SlotCount) + target;
				row[target] = index < block.Count ? block[index] : (ushort)0;
			}

			return row;
		}

		/// <summary>
		/// Lists the players a row has an embassy with.
		/// </summary>
		/// <param name="row">The 8 flag words of one player.</param>
		/// <returns>The player slots whose flag word has <see cref="EmbassyFlag"/> set.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public List<byte> EmbassiesOf(IReadOnlyList<ushort> row)
		{
			ArgumentNullException.ThrowIfNull(row);

			return [.. Enumerable.Range(0, Math.Min(row.Count, SlotCount))
				.Where(target => (row[target] & EmbassyFlag) != 0)
				.Select(target => (byte)target)];
		}

		/// <summary>
		/// Builds the SVE diplomacy block from each player's embassies.
		/// </summary>
		/// <param name="embassies">Embassy targets per player slot; missing slots stay 0.</param>
		/// <returns>64 flag words with only <see cref="EmbassyFlag"/> set where an embassy exists.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public ushort[] ToSve(IReadOnlyList<IEnumerable<byte>> embassies)
		{
			ArgumentNullException.ThrowIfNull(embassies);

			ushort[] block = new ushort[SlotCount * SlotCount];
			for (int player = 0; player < SlotCount && player < embassies.Count; player++)
			{
				foreach (byte target in embassies[player].Where(t => t < SlotCount))
				{
					block[(player * SlotCount) + target] = EmbassyFlag;
				}
			}

			return block;
		}
	}
}
