// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	public sealed class PreviewPalaceRenderer(PreviewPalaceResourcesDelegate resources) : IPreviewPalaceRenderer
	{
		private const int PART_WIDTH = 7;
		private const int PART_HEIGHT = 12;

		// Maps PalaceData index (0–6) to the corresponding sprite part.
		// Indices 1 and 2 share the WallLeft sprite; indices 4 and 5 share the WallRight sprite.
		private static readonly PreviewPalacePart[] IndexToPart =
		[
			PreviewPalacePart.Left,
			PreviewPalacePart.WallLeft,
			PreviewPalacePart.WallLeft,
			PreviewPalacePart.Center,
			PreviewPalacePart.WallRight,
			PreviewPalacePart.WallRight,
			PreviewPalacePart.Right,
		];

		public IBitmap RenderPalace(IPalaceData palace)
		{
			int partCount = CountActiveParts(palace);
			if (partCount == 0)
				return new Picture(1, 1);

			Picture result = new(partCount * PART_WIDTH, PART_HEIGHT);
			int xPos = 0;

			for (int i = 0; i < 7; i++)
			{
				byte level = palace.GetPalaceLevel(i);
				if (level == 0) continue;

				result.AddLayer(resources.GetPreviewPart(IndexToPart[i], level), xPos, 0);
				xPos += PART_WIDTH;
			}

			return result;
		}

		private static int CountActiveParts(IPalaceData palace)
		{
			int count = 0;
			for (int i = 0; i < 7; i++)
			{
				if (palace.GetPalaceLevel(i) > 0) 
				{
					count++;
				}
			}
			return count;
		}
	}
}
