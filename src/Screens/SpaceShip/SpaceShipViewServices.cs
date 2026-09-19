using CivOne.Graphics;
using CivOne.Screens.SpaceShipAssets;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Services.SpaceShip;

namespace CivOne.Screens
{
	/// <summary>
	/// Resource abstraction used by <see cref="SpaceShipView"/> for bitmap and font access.
	/// </summary>
	public interface ISpaceShipResourceService : IResourceFileBitmapProvider, IResourceFontHeightProvider
	{
	}

	/// <summary>
	/// Aggregates all dependencies required to construct <see cref="SpaceShipView"/>.
	/// </summary>
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

	/// <summary>
	/// Adapts generic resource services to <see cref="ISpaceShipResourceService"/> for spaceship rendering.
	/// </summary>
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