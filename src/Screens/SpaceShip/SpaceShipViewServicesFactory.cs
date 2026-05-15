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
