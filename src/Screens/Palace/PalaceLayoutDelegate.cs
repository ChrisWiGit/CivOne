using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

namespace CivOne.Screens.PalaceAssets
{
	/// <summary>
	/// One sprite of the palace screen.
	/// </summary>
	/// <param name="Level">The picture level the sprite is cut from (the level of its own section).</param>
	/// <param name="Style">The style the sprite is cut from.</param>
	/// <param name="Kind">The sprite.</param>
	/// <param name="X">The left edge on the 320×200 screen.</param>
	/// <param name="Y">The top edge on the 320×200 screen.</param>
	public readonly record struct PalacePiece(int Level, PalaceStyle Style, PalacePieceKind Kind, int X, int Y);

	/// <summary>
	/// Lays out the palace screen like the original game.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The original works on 9 sections; CivOneX section <c>k</c> is original section <c>k + 1</c>, and the outer
	/// sections 0 and 8 stay empty. A section that is not built has level -1; the base sections 3..5 start at 0.
	/// </para>
	/// <para>
	/// Every section has a role: section 4 is the center, a section is a left or right end when it is the outermost
	/// built one, and the others are inner sections. An end is only a tower (sections 3 and 5 add a wall half),
	/// an inner section is two wall halves. The sprites are placed from left to right; each sprite sits
	/// <c>offset[level][style][sprite] + 36 + level</c> pixels from the top, so higher levels sit lower.
	/// </para>
	/// <para>
	/// Like the original, the end tower is drawn a second time behind the wall of its inner neighbour,
	/// 7 pixels to the right and 2 pixels higher, when both sections share a style.
	/// </para>
	/// </remarks>
	internal sealed class PalaceLayoutDelegate
	{
		/// <summary>The number of original sections.</summary>
		public const int SectionCount = 9;

		private const int CenterSection = 4;
		private const int BaseY = 36;
		private const int CenterAdvance = 48;
		private const int WallAdvance = 24;
		private const int TowerAdvance = 25;
		private const int EmptyAdvance = 48;
		private const int PerspectiveTowerX = 7;
		private const int PerspectiveTowerY = -2;

		// Vertical offset per level, style (medieval, classical, islamic) and sprite
		// (center, wall shadow, wall, left tower, right tower).
		private static readonly int[,,] VerticalOffset =
		{
			{ { 1, 1, 1, 1, 1 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
			{ { 1, 0, 0, 2, 2 }, { 1, 0, 0, 1, 1 }, { 1, 0, 0, 2, 2 } },
			{ { 1, 0, 0, 2, 2 }, { 1, 0, 0, 1, 1 }, { 1, 0, 0, 2, 2 } },
			{ { 1, 3, 3, 2, 2 }, { 1, 0, 0, 1, 1 }, { 1, 0, 0, 2, 2 } },
			{ { 1, 3, 3, 2, 2 }, { 1, 0, 0, 1, 1 }, { 1, 0, 0, 2, 2 } },
		};

		private enum Role
		{
			LeftEnd,
			Inner,
			Center,
			RightEnd,
		}

		/// <summary>
		/// Lays out the palace screen.
		/// </summary>
		/// <param name="palace">The palace.</param>
		/// <returns>The sprites in drawing order; later sprites cover earlier ones.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public IReadOnlyList<PalacePiece> Layout(IPalaceData palace)
		{
			ArgumentNullException.ThrowIfNull(palace);

			Sections sections = new(palace);
			List<PalacePiece> pieces = [];

			int x = 16;
			if (sections.Level(2) == -1)
			{
				x -= WallAdvance;
			}
			if (sections.IsClassical(CenterSection))
			{
				x++;
			}

			int rightmost = SectionCount - 1;
			while (sections.Level(rightmost) == -1)
			{
				rightmost--;
			}
			int leftmost = 0;

			for (int i = 1; i < SectionCount - 1; i++)
			{
				int level = sections.Level(i);
				if (level == -1)
				{
					x += EmptyAdvance;
					continue;
				}

				switch (RoleOf(sections, i))
				{
					case Role.LeftEnd:
						if (i == 3)
						{
							sections.Add(pieces, level, i, x + TowerAdvance, BaseY, second: true);
						}
						// A classical end tower other than section 3 is drawn after all other sprites.
						if (!sections.IsClassical(i) || i == 3)
						{
							sections.Add(pieces, level, i, x, BaseY, second: false);
						}
						x += TowerAdvance + (i == 3 ? WallAdvance : 0);
						leftmost = i;
						break;

					case Role.Inner:
						if (i == leftmost + 1 && sections.Style(i) == sections.Style(leftmost))
						{
							sections.Add(pieces, level, leftmost, x + PerspectiveTowerX, BaseY + PerspectiveTowerY, second: false);
						}
						if (i == rightmost - 1 && sections.Style(i) == sections.Style(rightmost))
						{
							sections.Add(pieces, level, rightmost, x + PerspectiveTowerX, BaseY + PerspectiveTowerY, second: true);
						}
						sections.Add(pieces, level, i, x, BaseY, second: false);
						x += WallAdvance;
						sections.Add(pieces, level, i, x, BaseY, second: true);
						x += WallAdvance;
						break;

					case Role.Center:
						if (sections.IsClassical(i))
						{
							x--;
						}
						sections.Add(pieces, level, i, x - 2, BaseY, second: false);
						x += CenterAdvance;
						if (sections.IsClassical(i))
						{
							x--;
						}
						break;

					case Role.RightEnd:
						if (i == 5)
						{
							sections.Add(pieces, level, i, x, BaseY, second: false);
							x += WallAdvance;
						}
						AddRightTower(sections, pieces, level, i, x);
						x += TowerAdvance;
						break;
				}
			}

			if (leftmost != 3 && sections.IsClassical(leftmost))
			{
				int y = BaseY + sections.Level(leftmost + 1) - sections.Level(leftmost);
				sections.Add(pieces, sections.Level(leftmost), leftmost, (leftmost * 48) - 36, y, second: false);
			}

			return pieces;
		}

		/// <summary>
		/// Lays out the sprites of one section for the style preview, like the original's "Select new section." panel.
		/// </summary>
		/// <remarks>
		/// Like the original, the sprites depend only on the position of the section, not on its level or its
		/// neighbours' levels: a left end shows its tower and a shadowed wall (only the tower for section 1),
		/// a right end a plain wall and its tower (only the tower for section 7), and an inner section plain walls -
		/// one while the outermost section of its wing is not built, two otherwise.
		/// </remarks>
		/// <param name="palace">The palace.</param>
		/// <param name="section">The CivOneX section, 0..6.</param>
		/// <param name="style">The style to preview.</param>
		/// <param name="level">The level to preview.</param>
		/// <param name="left">The left edge of the sprites.</param>
		/// <returns>The sprites in drawing order.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public IReadOnlyList<PalacePiece> Preview(IPalaceData palace, int section, PalaceStyle style, int level, int left)
		{
			ArgumentNullException.ThrowIfNull(palace);

			Sections sections = new(palace);
			int i = section + 1;
			List<PalacePiece> pieces = [];

			switch (RoleOf(sections, i))
			{
				case Role.LeftEnd:
					if (i != 1)
					{
						pieces.Add(PreviewPiece(level, style, PalacePieceKind.WallShadow, left + TowerAdvance));
					}
					pieces.Add(PreviewPiece(level, style, PalacePieceKind.LeftTower, left));
					break;
				case Role.Center:
					pieces.Add(PreviewPiece(level, style, PalacePieceKind.Center, left));
					break;
				case Role.RightEnd:
					if (i != SectionCount - 2)
					{
						pieces.Add(PreviewPiece(level, style, PalacePieceKind.Wall, left));
					}
					pieces.Add(PreviewPiece(level, style, PalacePieceKind.RightTower, i != SectionCount - 2 ? left + WallAdvance : left));
					break;
				case Role.Inner:
					pieces.Add(PreviewPiece(level, style, PalacePieceKind.Wall, left));
					int outermost = i < CenterSection ? 1 : SectionCount - 2;
					if (sections.Level(outermost) != -1)
					{
						pieces.Add(PreviewPiece(level, style, PalacePieceKind.Wall, left + WallAdvance));
					}
					break;
			}

			return pieces;
		}

		/// <summary>
		/// Resolves a pressed section number like the original: a full section is refused, and a number of a section
		/// that cannot be built yet moves inwards to the next section that can.
		/// </summary>
		/// <param name="palace">The palace.</param>
		/// <param name="number">The section number on screen, 1..7.</param>
		/// <returns>The CivOneX section to upgrade, or <c>null</c> when the choice is refused.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public int? ResolveSection(IPalaceData palace, int number)
		{
			ArgumentNullException.ThrowIfNull(palace);
			if (number < 1 || number > SectionCount - 2)
			{
				return null;
			}

			Sections sections = new(palace);
			int s = number;
			if (sections.Level(s) >= 4)
			{
				return null;
			}

			while (s < CenterSection && sections.Level(s + 1) <= 0 && sections.Level(s) == -1)
			{
				s++;
			}
			while (s > CenterSection && sections.Level(s - 1) <= 0 && sections.Level(s) == -1)
			{
				s--;
			}

			return s - 1;
		}

		/// <summary>
		/// Gets the screen number of the section or garden under the mouse, like the original.
		/// </summary>
		/// <param name="x">The mouse x on the 320×200 screen.</param>
		/// <param name="y">The mouse y on the 320×200 screen.</param>
		/// <returns>The section number 1..7, or a garden as 'A'..'C'; <c>null</c> outside.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public char? ChoiceAt(int x, int y)
		{
			if (x < 0 || x >= 320 || y < 0 || y >= 200)
			{
				return null;
			}

			if (y >= 154)
			{
				int garden = x / 107;
				return garden is >= 0 and <= 2 ? (char)('A' + garden) : null;
			}

			int number = ((x + 12) / 48) + 1;
			return number is >= 1 and <= 7 ? (char)('0' + number) : null;
		}

		private static Role RoleOf(Sections sections, int i)
		{
			if (i == CenterSection)
			{
				return Role.Center;
			}
			if (i < CenterSection)
			{
				return i == 1 || sections.Level(i - 1) == -1 ? Role.LeftEnd : Role.Inner;
			}

			return i == 7 || sections.Level(i + 1) == -1 ? Role.RightEnd : Role.Inner;
		}

		private static void AddRightTower(Sections sections, List<PalacePiece> pieces, int level, int i, int x)
		{
			if (sections.IsClassical(i) && sections.Level(i - 1) > level)
			{
				int y = BaseY + sections.Level(i - 1) - level + 1;
				sections.Add(pieces, level, i, x - 3, y, second: true);
				return;
			}

			sections.Add(pieces, level, i, x - (sections.IsClassical(i) ? 3 : 2), BaseY, second: true);
		}

		private static PalacePiece PreviewPiece(int level, PalaceStyle style, PalacePieceKind kind, int x)
			=> new(level, style, kind, x, VerticalOffset[level, StyleIndex(style), (int)kind] + 1);

		private static int StyleIndex(PalaceStyle style) => style switch
		{
			PalaceStyle.Classical => 1,
			PalaceStyle.Islamic => 2,
			_ => 0,
		};

		/// <summary>
		/// The palace on the original's 9 section scale.
		/// </summary>
		private sealed class Sections
		{
			private readonly int[] _levels = new int[SectionCount];
			private readonly PalaceStyle[] _styles = new PalaceStyle[SectionCount];

			public Sections(IPalaceData palace)
			{
				Array.Fill(_levels, -1);
				Array.Fill(_styles, PalaceStyle.Medieval);
				for (int k = 0; k < 7; k++)
				{
					int level = palace.GetPalaceLevel(k);
					bool isBase = k >= 2 && k <= 4;
					_levels[k + 1] = level == 0 && !isBase ? -1 : level;
					PalaceStyle style = palace.GetPalaceStyle(k);
					_styles[k + 1] = style == PalaceStyle.None ? PalaceStyle.Medieval : style;
				}
			}

			public int Level(int i) => i is >= 0 and < SectionCount ? _levels[i] : -1;

			public PalaceStyle Style(int i) => _styles[i];

			public bool IsClassical(int i) => _styles[i] == PalaceStyle.Classical;

			/// <summary>
			/// Gets the two sprites of a section. The palace screen draws the wall half of an end only for
			/// sections 3 and 5.
			/// </summary>
			public (PalacePieceKind? First, PalacePieceKind? Second) Pieces(int i)
			{
				int level = _levels[i];
				return RoleOf(this, i) switch
				{
					Role.LeftEnd => (PalacePieceKind.LeftTower, EndWall(i == 3, level > 0)),
					Role.Inner => (WallKind(Level(i - 1) > level || i == 5 || Level(i - 2) == -1), PalacePieceKind.Wall),
					Role.Center => (PalacePieceKind.Center, null),
					_ => (EndWall(i == 5, Level(i - 1) > level || i == 5), PalacePieceKind.RightTower),
				};
			}

			private static PalacePieceKind WallKind(bool shadow) => shadow ? PalacePieceKind.WallShadow : PalacePieceKind.Wall;

			private static PalacePieceKind? EndWall(bool shown, bool shadow) => shown ? WallKind(shadow) : null;

			/// <summary>
			/// Adds one sprite of section <paramref name="i"/>, placed for <paramref name="level"/>.
			/// The perspective tower uses the level of the inner section but the sprite of the end section.
			/// </summary>
			public void Add(List<PalacePiece> pieces, int level, int i, int x, int baseY, bool second)
			{
				(PalacePieceKind? first, PalacePieceKind? other) = Pieces(i);
				if ((second ? other : first) is not PalacePieceKind kind)
				{
					return;
				}

				int y = VerticalOffset[level, StyleIndex(_styles[i]), (int)kind] + baseY + level;
				pieces.Add(new(_levels[i], _styles[i], kind, x, y));
			}
		}
	}
}
