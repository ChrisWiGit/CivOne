using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Maps colours onto the closest entry of a given palette.
	/// <br/>
	/// The engine stores pictures as palette indices, so an image that does not already use the
	/// palette it is drawn with has to be translated. This is the lossy direction: a colour the
	/// palette does not contain is replaced by its nearest neighbour.
	/// <br/>
	/// Distance is measured as the squared euclidean distance in RGB. That is not how the eye works,
	/// but it is predictable, and art that was drawn against a known palette in the first place
	/// usually matches exactly anyway.
	/// </summary>
	internal sealed class PaletteMapperDelegate
	{
		/// <summary>
		/// Pixels less opaque than this are treated as fully transparent.
		/// </summary>
		private const int OpacityThreshold = 128;

		/// <summary>
		/// Returned by <see cref="FindTransparentIndex"/> when the palette holds no transparent entry.
		/// </summary>
		private const int NoTransparentEntry = -1;

		/// <summary>
		/// Maps a single colour onto the closest palette entry.
		/// </summary>
		/// <param name="colour">The colour to map.</param>
		/// <param name="palette">The palette to map into.</param>
		/// <returns>The index of the closest entry.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public byte MapColour(Colour colour, Palette palette)
		{
			ArgumentNullException.ThrowIfNull(palette);

			Colour[] entries = Snapshot(palette);
			return MapColour(colour, entries, FindTransparentIndex(entries));
		}

		/// <summary>
		/// Maps a whole image onto a palette.
		/// </summary>
		/// <param name="pixels">The colours to map, one per pixel.</param>
		/// <param name="palette">The palette to map into.</param>
		/// <returns>One palette index per pixel, in the same order.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public byte[] MapPixels(ReadOnlySpan<Colour> pixels, Palette palette)
		{
			ArgumentNullException.ThrowIfNull(palette);

			Colour[] entries = Snapshot(palette);
			int transparent = FindTransparentIndex(entries);

			// Real images repeat their colours, so a search result is remembered per colour rather
			// than per pixel. The key keeps the opacity decision, because a transparent and an opaque
			// pixel of the same colour do not map to the same entry.
			Dictionary<int, byte> cache = [];
			byte[] indices = new byte[pixels.Length];

			for (int i = 0; i < pixels.Length; i++)
			{
				Colour colour = pixels[i];
				int key = (colour.A < OpacityThreshold ? 1 << 24 : 0) | (colour.R << 16) | (colour.G << 8) | colour.B;
				if (!cache.TryGetValue(key, out byte index))
				{
					index = MapColour(colour, entries, transparent);
					cache[key] = index;
				}
				indices[i] = index;
			}

			return indices;
		}

		/// <summary>
		/// Maps the entries of a source palette onto a target palette.
		/// <br/>
		/// An indexed image can then be translated through the resulting table instead of mapping
		/// every single pixel, which is far cheaper because a palette has at most 256 entries.
		/// </summary>
		/// <param name="source">The palette the image currently uses.</param>
		/// <param name="palette">The palette to map into.</param>
		/// <returns>A lookup table from source index to target index.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public byte[] MapPalette(ReadOnlySpan<Colour> source, Palette palette)
		{
			ArgumentNullException.ThrowIfNull(palette);

			Colour[] entries = Snapshot(palette);
			int transparent = FindTransparentIndex(entries);

			byte[] table = new byte[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				table[i] = MapColour(source[i], entries, transparent);
			}
			return table;
		}

		private static byte MapColour(Colour colour, Colour[] entries, int transparent)
		{
			if (colour.A < OpacityThreshold && transparent != NoTransparentEntry)
			{
				return (byte)transparent;
			}

			byte best = 0;
			int bestDistance = int.MaxValue;
			for (int i = 0; i < entries.Length; i++)
			{
				// A transparent entry carries no usable colour, so it is never chosen by distance.
				if (entries[i].A < OpacityThreshold)
				{
					continue;
				}

				int red = colour.R - entries[i].R;
				int green = colour.G - entries[i].G;
				int blue = colour.B - entries[i].B;
				int distance = (red * red) + (green * green) + (blue * blue);
				if (distance >= bestDistance)
				{
					continue;
				}

				best = (byte)i;
				bestDistance = distance;
				if (distance == 0)
				{
					break;
				}
			}
			return best;
		}

		/// <summary>
		/// Copies the palette out of unmanaged memory once, so the search does not pay for the bounds
		/// checks of the palette indexer on every comparison.
		/// </summary>
		private static Colour[] Snapshot(Palette palette)
		{
			Colour[] entries = new Colour[Math.Min(palette.Length, 256)];
			for (int i = 0; i < entries.Length; i++)
			{
				entries[i] = palette[i];
			}
			return entries;
		}

		private static int FindTransparentIndex(Colour[] entries)
		{
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].A < OpacityThreshold)
				{
					return i;
				}
			}
			return NoTransparentEntry;
		}
	}
}
