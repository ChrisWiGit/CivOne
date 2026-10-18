using System;
using CivOne.Enums;

namespace CivOne.Screens.PalaceAssets
{
	/// <summary>
	/// The mini palace drawn in the side bar and by the Historian, laid out like the original game:
	/// 9 slots on a fixed 7 pixel grid.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A slot level of -1 means "no section", 0 means "base section, not drawn", and 1 to 4 are drawn with
	/// sprite stage <c>level - 1</c>.
	/// Slot <c>k + 1</c> holds the real palace section <c>k</c>; slots 0 and 8 exist only for generated AI palaces.
	/// </para>
	/// <para>
	/// The part per slot follows the original's neighbour rule: slot 4 is the center, a slot left of it is the left
	/// edge when it is slot 1 or its left neighbour is missing, otherwise an inner wall; the right side mirrors this.
	/// The original reads undefined memory for the outer neighbour of slots 0 and 8; both are drawn as inner walls.
	/// </para>
	/// </remarks>
	public sealed class MiniPalaceLayout
	{
		/// <summary>The number of slots.</summary>
		public const int SlotCount = 9;

		/// <summary>The width of one slot in pixels.</summary>
		public const int SlotWidth = 7;

		/// <summary>The width of the full grid in pixels.</summary>
		public const int Width = SlotCount * SlotWidth;

		private const int CenterSlot = 4;
		private const int MaxLevel = 4;

		private readonly sbyte[] _levels;
		private readonly PalaceStyle[] _styles;

		/// <summary>
		/// Creates a layout from slot levels and styles.
		/// </summary>
		/// <param name="levels">9 slot levels, -1 to 4; higher values are capped at 4.</param>
		/// <param name="styles">9 slot styles.</param>
		/// <exception cref="ArgumentException">When an array does not have 9 entries.</exception>
		public MiniPalaceLayout(int[] levels, PalaceStyle[] styles)
		{
			ArgumentNullException.ThrowIfNull(levels);
			ArgumentNullException.ThrowIfNull(styles);
			if (levels.Length != SlotCount || styles.Length != SlotCount)
			{
				throw new ArgumentException($"A mini palace needs {SlotCount} levels and styles.");
			}

			_levels = new sbyte[SlotCount];
			for (int i = 0; i < SlotCount; i++)
			{
				_levels[i] = (sbyte)Math.Clamp(levels[i], -1, MaxLevel);
			}
			_styles = [.. styles];
		}

		/// <summary>
		/// Gets a layout without any section, as drawn for a player without cities.
		/// </summary>
		public static MiniPalaceLayout Empty { get; } = new([-1, -1, -1, -1, -1, -1, -1, -1, -1], new PalaceStyle[SlotCount]);

		/// <summary>
		/// Converts a real palace into the 9 slot layout.
		/// </summary>
		/// <param name="palace">The palace; its section <c>k</c> becomes slot <c>k + 1</c>.</param>
		/// <returns>
		/// The layout. The base sections 2..4 keep level 0; an unbuilt outer section becomes -1, because the original
		/// raises new sections directly from -1 to 1.
		/// </returns>
		public static MiniPalaceLayout FromPalace(IPalaceData palace)
		{
			ArgumentNullException.ThrowIfNull(palace);

			int[] levels = [-1, -1, -1, -1, -1, -1, -1, -1, -1];
			PalaceStyle[] styles = new PalaceStyle[SlotCount];
			for (int k = 0; k < 7; k++)
			{
				int level = palace.GetPalaceLevel(k);
				bool isBase = k >= 2 && k <= 4;
				levels[k + 1] = level == 0 && !isBase ? -1 : level;
				styles[k + 1] = palace.GetPalaceStyle(k);
			}

			return new(levels, styles);
		}

		/// <summary>
		/// Gets the level of a slot.
		/// </summary>
		/// <param name="slot">The slot, 0..8.</param>
		/// <returns>-1 for no section, otherwise 0..4.</returns>
		public int Level(int slot) => _levels[slot];

		/// <summary>
		/// Gets the style of a slot.
		/// </summary>
		/// <param name="slot">The slot, 0..8.</param>
		/// <returns>The style.</returns>
		public PalaceStyle Style(int slot) => _styles[slot];

		/// <summary>
		/// Gets whether a slot is drawn.
		/// </summary>
		/// <param name="slot">The slot, 0..8.</param>
		/// <returns><c>true</c> when its level is above 0.</returns>
		public bool IsVisible(int slot) => _levels[slot] > 0;

		/// <summary>
		/// Gets whether any slot is drawn.
		/// </summary>
		public bool HasVisibleSlot
		{
			get
			{
				for (int i = 0; i < SlotCount; i++)
				{
					if (IsVisible(i))
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Gets the sprite part of a slot by the original's neighbour rule.
		/// </summary>
		/// <param name="slot">The slot, 0..8.</param>
		/// <returns>The part to draw.</returns>
		public PreviewPalacePart PartAt(int slot)
		{
			if (slot == CenterSlot)
			{
				return PreviewPalacePart.Center;
			}
			if (slot < CenterSlot)
			{
				bool isEdge = slot == 1 || (slot > 0 && _levels[slot - 1] == -1);
				return isEdge ? PreviewPalacePart.Left : PreviewPalacePart.WallLeft;
			}

			bool isRightEdge = slot == 7 || (slot < SlotCount - 1 && _levels[slot + 1] == -1);
			return isRightEdge ? PreviewPalacePart.Right : PreviewPalacePart.WallRight;
		}
	}
}
