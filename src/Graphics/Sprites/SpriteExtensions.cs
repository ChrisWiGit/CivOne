// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;

namespace CivOne.Graphics.Sprites
{
	public static class SpriteExtensions
	{
		public static IBitmap? ToBitmap(this ISprite? sprite)
		{
			if (sprite == null)
			{
				return null;
			}
			Debug.Assert(sprite.Bitmap != null, "Sprite's Bitmap is null");
			ArgumentNullException.ThrowIfNull(sprite.Bitmap, nameof(sprite));
			
			using var defaultPalette = Common.DefaultPalette;
			return new Picture(sprite.Bitmap, defaultPalette);
		}
	}
}