namespace CivOne.Units.TribalHuts
{
	public class MetalDepositsEventHandler : ITribalHutEventHandler
	{
		private readonly Player player;

		public MetalDepositsEventHandler(Player player)
		{
			this.player = player;
		}

		public string[] GetEventMessage()
		{
			return
			[
				"You have discovered",
				"valuable metal deposits",
				"worth 50$"
			];
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