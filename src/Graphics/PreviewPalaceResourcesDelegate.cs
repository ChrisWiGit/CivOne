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
using CivOne.Enums;

namespace CivOne.Graphics
{
	public sealed class PreviewPalaceResourcesWrapper(Func<string, Picture> getPictureByName)
	{
		private const int BASE_X = 160;
		private const int LEVEL_STRIDE = 35;  // 5 parts × 7px
		private const int PART_WIDTH = 7;
		private const int PREVIEW_H = 14;

		private readonly Func<string, Picture> _getPictureByName = getPictureByName ?? throw new ArgumentNullException(nameof(getPictureByName));
		private readonly Dictionary<int, Picture> _cache = [];

		private static int GetStyleY(PalaceStyle style) => style switch
		{
			PalaceStyle.Medieval  => 1,
			PalaceStyle.Classical => 17,
			PalaceStyle.Islamic   => 32,
			_ => throw new ArgumentOutOfRangeException(nameof(style), $"Unexpected palace style: {style}"),
		};

		internal void ClearCache() => _cache.Clear();

		internal Picture GetPreviewPart(PreviewPalacePart part, int level, PalaceStyle style)
		{
			int key = (int)style * 1000 + level * 10 + (int)part;
			if (_cache.TryGetValue(key, out Picture? cached))
				return cached;

			int x = BASE_X + ((level - 1) * LEVEL_STRIDE) + ((int)part * PART_WIDTH);
			int y = GetStyleY(style);
			Picture result = _getPictureByName("SP257")[x, y, PART_WIDTH, PREVIEW_H];
			_cache[key] = result;
			return result;
		}
	}
}
