using CivOne.Graphics;
using CivOne.Screens.SpaceShipAssets;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Services.SpaceShip;

namespace CivOne.Screens
{
	/// <summary>
	/// Creates the default dependency graph for <see cref="SpaceShipView"/>.
	/// </summary>
	public static class SpaceShipViewServicesFactory
	{
		public static SpaceShipViewServices CreateDefault(ITranslationService translationService)
		{
			return new SpaceShipViewServices
			{
				SpaceShipServiceFactory = SpaceShipServiceFactoryProvider.GetInstance(),
				DebugSpaceShipServiceFactory = SpaceShipServiceFactoryProvider.GetDebugInstance(),
				SpaceShipSpriteProvider = SpaceShipSpriteProviderFactory.GetInstance(),
				SlotBlueprint = SpaceShipSlotBlueprintFactoryProvider.GetInstance().Create(),
				Resources = new SpaceShipResourceServiceAdapter(Resources.Instance, Resources.Instance),
				CalendarService = new GameCalendarService(translationService),
				RandomService = RandomServiceFactory.Create()
			};
		}
	}
}