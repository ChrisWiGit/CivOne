namespace CivOne.Services.Palace
{
	internal static class PalaceUpgradeServiceFactory
	{
		private static IPalaceUpgradeService? _instance;

		public static IPalaceUpgradeService GetInstance()
		{
			if (_instance == null)
			{
				_instance = new PalaceUpgradeService(
				[
					new HumanCivScorePalaceTrigger()
					// additional triggers can be added here in the future if desired
				]);
			}

			return _instance;
		}
	}
}