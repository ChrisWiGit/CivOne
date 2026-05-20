// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Graphics;

namespace CivOne.Screens
{
	internal sealed class BorderDrawDelegate
	{
			private readonly int _borderTileSize = 8;

			public int BorderTileSize => _borderTileSize;

		public void Draw(BaseScreen screen, int border, int width, int height, Func<int, int, int, int, Picture> getBorderSprite)
		{
			border %= 2;
			Picture[] borders = new Picture[8];
			int index = 0;
			for (int yy = 0; yy < 2; yy++)
				for (int xx = 0; xx < 4; xx++)
				{
					// TODO: replace by IResource, when available and remove getBorderSprite parameter. 
					borders[index] = getBorderSprite(((border == 0) ? 192 : 224) + (BorderTileSize * xx), 120 + (BorderTileSize * yy), BorderTileSize, BorderTileSize);
					index++;
				}

			for (int x = BorderTileSize; x < width - BorderTileSize; x += BorderTileSize)
			{
				screen.AddLayer(borders[4], x, 0)
					.AddLayer(borders[6], x, height - BorderTileSize);
			}
			for (int y = BorderTileSize; y < height - BorderTileSize; y += BorderTileSize)
			{
				screen.AddLayer(borders[7], 0, y)
					.AddLayer(borders[5], width - BorderTileSize, y);
			}
			screen.AddLayer(borders[0], 0, 0)
				.AddLayer(borders[1], width - BorderTileSize, 0)
				.AddLayer(borders[2], 0, height - BorderTileSize)
				.AddLayer(borders[3], width - BorderTileSize, height - BorderTileSize);
		}
	}
}
