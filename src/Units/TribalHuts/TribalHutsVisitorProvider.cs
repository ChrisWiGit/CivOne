using System.Diagnostics;

namespace CivOne.Units.TribalHuts
{
	/**
	 * Provides an instance of ITribalHutsVisitor based on the player type.
     * This is a refactoring for logic of visiting tribal huts.
	 * 1. Removing switches and using polymorphism
	 * 2. Separation of concerns
	 *  * If the player is human, it returns HumanTribalHutsVisitorImpl with dialog handling (UI separation)
	 *  * Separate behavior of different hut outcomes using handlers
	 *  * Separate additional conditions from outcome logic
	 * 3. Using Dependency Injection for providing dependencies (not fully possible due to existing code structure)
	 *
	 * Using a DI Container would be better, but this would require more extensive refactoring.
	 */
	internal class TribalHutsVisitorProvider
	{
		public static ITribalHutsVisitor provide(
			Player player,
			Map map,
			IUnit currentUnit,
			IGame gameInstance,
			ILoggerService logger,
			Random random)
		{
			Debug.Assert(player != null && map != null && currentUnit != null
				&& gameInstance != null
				&& logger != null
				&& random != null, "Invalid parameters provided to TribalHutsVisitorProvider");

			if (player.IsHuman)
			{
				return new HumanTribalHutsVisitorImpl(player, map, currentUnit, gameInstance, logger, random);
			}

			return new TribalHutsVisitorImpl(player, map, currentUnit, gameInstance, logger, random);
		}
	}
}
