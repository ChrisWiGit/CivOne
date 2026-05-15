// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;
using CivOne.Screens.SpaceShipAssets;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Services.SpaceShip;

namespace CivOne.Screens
{
	public interface ISpaceShipResourceService : IResourceFileBitmapProvider, IResourceFontHeightProvider
	{
	}

	public sealed class SpaceShipViewServices
	{
		public required ISpaceShipServiceFactory SpaceShipServiceFactory { get; init; }
		public required ISpaceShipServiceFactory DebugSpaceShipServiceFactory { get; init; }
		public required ISpaceShipSpriteProvider SpaceShipSpriteProvider { get; init; }
		public required ISpaceShipSlotBlueprint SlotBlueprint { get; init; }
		public required ISpaceShipResourceService Resources { get; init; }
		public required IGameCalendarService CalendarService { get; init; }
		public required IRandomService RandomService { get; init; }
	}

	internal sealed class SpaceShipResourceServiceAdapter(IResourceFileBitmapProvider bitmapProvider, IResourceFontHeightProvider fontHeightProvider) : ISpaceShipResourceService
	{
		private readonly IResourceFileBitmapProvider _bitmapProvider = bitmapProvider;
		private readonly IResourceFontHeightProvider _fontHeightProvider = fontHeightProvider;

		public IBitmap this[string filename] => _bitmapProvider[filename];

		public bool Exists(string filename)
		{
			return _bitmapProvider.Exists(filename);
		}

		public int GetFontHeight(int FontId) => _fontHeightProvider.GetFontHeight(FontId);
	}
}
