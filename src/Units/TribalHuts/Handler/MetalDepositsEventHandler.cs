using CivOne.Services;

namespace CivOne.Units.TribalHuts
{
	public class MetalDepositsHandler : ITribalHutEventHandler
	{
		private readonly Player player;

		public MetalDepositsHandler(Player player)
		{
			this.player = player;
		}

		public string[] GetEventMessage()
		{
			return TranslationServiceFactory.GetCurrent().TranslateFormattedArray("You have discovered\nvaluable metal deposits\nworth 50$");
		}

		public void PreExecute()
		{
			// No pre-execution logic needed for this event
		}

		public void PostExecute()
		{
			player.Gold += 50;
		}
	}
}