// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.IO;

namespace CivOne.Graphics
{
	public class Palette : BaseUnmanaged
	{
		public int Length => base.Size / 4;

		private int ToInt(int index) => ReadInt(index * 4);

		public Colour this[int index]
		{
			get
			{
				return new Colour(
					ReadByte((index * 4)),
					ReadByte((index * 4) + 1),
					ReadByte((index * 4) + 2),
					ReadByte((index * 4) + 3)
				);
			}
			set
			{
				WriteByte((index * 4), value.A);
				WriteByte((index * 4) + 1, value.R);
				WriteByte((index * 4) + 2, value.G);
				WriteByte((index * 4) + 3, value.B);
			}
		}

		public IEnumerable<Colour> Entries => Enumerable.Range(0, Length).Select(x => this[x]);

		public Palette Copy() => Copy(this);

		/// <summary>
		/// Merges the specified palette into this palette, starting at the specified index and for the specified count. 
		/// If startIndex or count are not specified, the entire source palette will be merged into this palette starting at index 0.
		/// The merge operation will overwrite the colors in this palette with the colors from the source palette for the specified range.
		/// 
		/// Use case: When you have a sprite or bitmap with its own palette, and you want to use that palette for a screen, you can merge the sprite's palette into a copy of the default palette and set that as the screen's palette. 
		/// This allows you to use the colors from the sprite's palette without affecting the default palette for other screens.
		/// 
		/// This Methode will modify the current palette, so you should create a copy of the default palette and merge into that copy, to avoid modifying the default palette for all screens.
		/// You should use <see cref="Copy"/> to create a copy of the default palette before merging, to avoid modifying the default palette for all screens.
		/// </summary>
		/// <param name="source">The source palette to merge into this palette.</param>
		/// <param name="startIndex">The index in this palette at which to start merging the source palette. If not specified, the merge will start at index 0.</param>
		/// <param name="count">The number of colors from the source palette to merge into this palette. If not specified, the entire source palette will be merged.</param>
		/// <returns>The merged current palette (this).</returns>
		/// <example>
		/// <code>
		/// // Create a copy of the default palette and merge the sprite palette into it
		/// Palette palette = Common.DefaultPalette.Copy().Merge(sprite.Palette);
		/// // Set the palette for the screen to the merged palette
		/// screen.SetPalette(palette);
		/// </code>
		/// </example>
		public Palette Merge(Palette source, int startIndex = -1, int count = -1)
		{
			if (startIndex == -1) startIndex = 0;
			if (count == -1) count = Length - startIndex;
			for (int i = startIndex; i < startIndex + count && i < Length && i < source.Length; i++)
			{
				WriteInt(i * 4, source.ToInt(i));
			}
			return this;
		}

		[Obsolete("Use Merge(...) instead.")]
		public Palette MergePalette(Palette source, int startIndex = -1, int count = -1) => Merge(source, startIndex, count);

		public static Palette Copy(Palette source) => new(source);

		public static implicit operator Palette(Colour[] palette) => new(palette);

		private Palette(Colour[] palette) : this(palette.Length)
		{
			for (int i = 0; i < Length; i++)
				this[i] = palette[i];
		}

		private Palette(Palette source) : base(source)
		{
		}

		public Palette(int length = 256) : base(length * 4)
		{
		}
	}
}