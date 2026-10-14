using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Collapses CivOneX's live scores into the 8-slot score block of an SVE file.
	/// </summary>
	/// <remarks>
	/// The original's score routine writes the human score into slot 0, the barbarian scratch slot, and the turn loop
	/// zeroes every slot before reading it. So the block only has to match the layout, not the values:
	/// the human score goes to slot 0 and slots 1..7 carry each player's city sum, the value the original
	/// accumulates there for the Historian.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §11 (D8).
	/// </remarks>
	internal sealed class SveCivilizationScoreDelegate
	{
		/// <summary>
		/// The number of score slots in an SVE file.
		/// </summary>
		public const int SlotCount = 8;

		/// <summary>
		/// Builds the SVE score block.
		/// </summary>
		/// <param name="humanScore">The human player's live score.</param>
		/// <param name="citySums">City sum per player slot; slot 0 (barbarians) is ignored, missing slots stay 0.</param>
		/// <returns>8 slots: <paramref name="humanScore"/> in slot 0, the city sums clamped to 0..<see cref="short.MaxValue"/> in 1..7.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public ushort[] Collapse(ushort humanScore, IReadOnlyList<int>? citySums = null)
		{
			ushort[] slots = new ushort[SlotCount];
			slots[0] = humanScore;
			for (int slot = 1; slot < SlotCount && citySums != null && slot < citySums.Count; slot++)
			{
				slots[slot] = (ushort)Math.Clamp(citySums[slot], 0, short.MaxValue);
			}

			return slots;
		}
	}
}
