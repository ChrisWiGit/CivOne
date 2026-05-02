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
	internal sealed class ResourcesPalaceSpriteProvider : IPalaceSpriteProvider
	{
		private readonly Resources _resources;

		public ResourcesPalaceSpriteProvider(Resources resources)
		{
			_resources = resources;
		}

		public Picture GetBackground() => _resources["CBACK"];

		public Picture GetGardenBackdrop(byte gardenLevel)
		{
			return gardenLevel switch
			{
				1 => _resources["CBACKS1"],
				2 => _resources["CBACKS2"],
				3 => _resources["CBACKS3"],
				_ => null
			};
		}

		public Picture GetGardenBrush(int gardenIndex, byte gardenLevel)
		{
			if (gardenIndex == 0)
			{
				return gardenLevel switch
				{
					1 => _resources["CBRUSH0"],
					2 => _resources["CBRUSH2"],
					3 => _resources["CBRUSH4"],
					_ => null
				};
			}

			if (gardenIndex == 2)
			{
				return gardenLevel switch
				{
					1 => _resources["CBRUSH1"],
					2 => _resources["CBRUSH3"],
					3 => _resources["CBRUSH5"],
					_ => null
				};
			}

			return null;
		}

		public Picture GetPalacePart(PalaceStyle style, PalacePart part, int level)
			=> _resources.GetPalace(style, part, level);
	}
}