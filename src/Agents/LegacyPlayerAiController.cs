using System;
using System.Collections.Generic;
using CivOne.Units;

namespace CivOne.Agents
{
	/// <summary>
	/// Adapts the existing <see cref="AI"/> implementation to <see cref="IPlayerAiController"/>.
	/// </summary>
	internal sealed class LegacyPlayerAiController(Player player) : IPlayerAiController
	{
		/// <summary>
		/// Executes the legacy AI movement logic for one unit.
		/// </summary>
		/// <param name="unit">The unit to move.</param>
		public void Move(IUnit unit)
		{
			ArgumentNullException.ThrowIfNull(unit);
			AI.Instance(player).Move(unit);
		}

		/// <summary>
		/// Executes the legacy AI city production logic for one city.
		/// </summary>
		/// <param name="city">The city to manage.</param>
		public void CityProduction(City city)
		{
			ArgumentNullException.ThrowIfNull(city);
			AI.Instance(player).CityProduction(city);
		}

		/// <summary>
		/// Executes the legacy AI research selection logic.
		/// </summary>
		public void ChooseResearch()
		{
			AI.Instance(player).ChooseResearch();
		}
	}

	/// <summary>
	/// Caches one AI controller instance per runtime player.
	/// </summary>
	internal sealed class PlayerAiControllerFactory : IPlayerAiControllerFactory
	{
		private readonly Dictionary<Player, IPlayerAiController> _controllers = [];

		/// <summary>
		/// Gets the singleton factory instance.
		/// </summary>
		public static PlayerAiControllerFactory Instance { get; } = new();

		/// <summary>
		/// Gets the cached controller for the specified player or creates one.
		/// </summary>
		/// <param name="player">The player that needs an AI controller.</param>
		/// <returns>The cached or newly created controller.</returns>
		public IPlayerAiController CreateFor(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			// using Player as cache key is necessary, 
			// otherwise the factory can return a controller bound to an old Player instance (stale state / wrong AI target) 
			// if a game is loaded/recreated in the same process with the same persisted PlayerGuid.
			if (!_controllers.TryGetValue(player, out IPlayerAiController? controller))
			{
				controller = new LegacyPlayerAiController(player);
				_controllers[player] = controller;
			}

			return controller;
		}
	}
}