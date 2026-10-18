using CivOne.Services;

namespace CivOne.Screens
{
	/// <summary>
	/// Creates default dependencies for <see cref="SpaceShipCivilizationSelectorDialog"/>.
	/// </summary>
	public static class SpaceShipCivilizationSelectorServicesFactory
	{
		public static SpaceShipCivilizationSelectorServices CreateDefault()
		{
			return new SpaceShipCivilizationSelectorServices
			{
				SelectorService = new SpaceShipCivilizationSelectorService(new SpaceShipCivilizationEligibilityEvaluator()),
				TranslationService = TranslationServiceFactory.CreateDefault()
			};
		}
	}
}