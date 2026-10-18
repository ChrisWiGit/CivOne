using System;
using System.Collections.Generic;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Services.Fonts
{
	/// <summary>
	/// Default <see cref="IFontInspectionService"/> implementation reading the fonts that
	/// <see cref="Resources"/> loaded from <c>FONTS.CV</c>.
	/// </summary>
	internal sealed class FontInspectionService : IFontInspectionService
	{
		private readonly IReadOnlyList<IFont>? _fonts;

		/// <summary>
		/// The font list is resolved lazily, so constructing this service never forces the resource
		/// loader to initialise.
		/// </summary>
		private IReadOnlyList<IFont> Fonts => _fonts ?? Resources.Instance.Fonts;

		/// <summary>
		/// Creates the service.
		/// </summary>
		/// <param name="fonts">
		/// Fonts to inspect. When <see langword="null"/>, the fonts of the current
		/// <see cref="Resources"/> instance are used.
		/// </param>
		public FontInspectionService(IReadOnlyList<IFont>? fonts = null)
		{
			_fonts = fonts;
		}

		public int FontCount => Fonts.Count;

		public FontDescription? GetFont(int fontId)
		{
			if (!TryGetFont(fontId, out IFont? font))
			{
				return null;
			}

			return new FontDescription(fontId, font!.FontHeight, font.FirstChar, font.LastChar, FontSetFactory.IsInternationalFontSet(font));
		}

		public IReadOnlyList<byte> GetCharacterCodes(int fontId)
		{
			if (!TryGetFont(fontId, out IFont? font))
			{
				return [];
			}

			List<byte> codes = [];
			for (int code = font!.FirstChar; code <= font.LastChar; code++)
			{
				codes.Add((byte)code);
			}
			return codes;
		}

		public bool HasVisibleGlyph(int fontId, char character)
		{
			if (!TryGetFont(fontId, out IFont? font))
			{
				return false;
			}

			// GetLetter returns a freshly allocated bitmap here, unlike the cached variant in Resources,
			// so this instance is owned by the caller and must be disposed.
			using Bytemap glyph = font!.GetLetter(character, 1);
			for (int y = 0; y < glyph.Height; y++)
			{
				for (int x = 0; x < glyph.Width; x++)
				{
					if (glyph[x, y] != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool TryGetFont(int fontId, out IFont? font)
		{
			font = null;
			IReadOnlyList<IFont> fonts = Fonts;
			if (fontId < 0 || fontId >= fonts.Count)
			{
				return false;
			}

			font = fonts[fontId];
			return true;
		}
	}
}
