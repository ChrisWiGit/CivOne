// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Default factory that wires one player to shared spaceship rule services.
	/// </summary>
	public class SpaceShipServiceFactory(
		ISpaceShipPlacementRules placementRules,
		ISpaceShipLaunchRules launchRules,
		ISpaceShipScreenDataFactory screenDataFactory) : ISpaceShipServiceFactory
	{
		private readonly ISpaceShipPlacementRules _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
		private readonly ISpaceShipLaunchRules _launchRules = launchRules ?? throw new ArgumentNullException(nameof(launchRules));
		private readonly ISpaceShipScreenDataFactory _screenDataFactory = screenDataFactory ?? throw new ArgumentNullException(nameof(screenDataFactory));

		public ISpaceShipService Create(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);
			return new SpaceShipService(player, _placementRules, _launchRules, _screenDataFactory);
		}
	}

	/// <summary>
	/// Singleton provider for production and debug <see cref="ISpaceShipServiceFactory"/> instances.
	/// </summary>
	public static class SpaceShipServiceFactoryProvider
	{
		private static ISpaceShipServiceFactory? _instance;
		private static ISpaceShipServiceFactory? _debugInstance;

		public static ISpaceShipServiceFactory GetInstance()
		{
			ISpaceShipSlotBlueprint slotBlueprint = SpaceShipSlotBlueprintFactoryProvider.GetInstance().Create();
			_instance ??= new SpaceShipServiceFactory(
					new SpaceShipPlacementRules(slotBlueprint),
					new SpaceShipLaunchRules(),
					new SpaceShipScreenDataFactory(slotBlueprint));

			return _instance;
		}

		public static ISpaceShipServiceFactory GetDebugInstance()
		{
			ISpaceShipSlotBlueprint slotBlueprint = SpaceShipSlotBlueprintFactoryProvider.GetInstance().Create();
			_debugInstance ??= new SpaceShipServiceFactory(
					new DebugSpaceShipPlacementRules(slotBlueprint),
					new DebugSpaceShipLaunchRules(),
					new SpaceShipScreenDataFactory(slotBlueprint));

			return _debugInstance;
		}
	}
}
