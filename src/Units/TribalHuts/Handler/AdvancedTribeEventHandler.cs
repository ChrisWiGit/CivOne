using CivOne.Services;
using CivOne.Tasks;

namespace CivOne.Units.TribalHuts
{
	public class AdvancedTribeEventHandler : ITribalHutEventHandler
	{
		private readonly int X;
		private readonly int Y;

		private readonly Player player;

		public AdvancedTribeEventHandler(int x, int y, Player player)
		{
			this.player = player;
			X = x;
			Y = y;
		}

		public string[] GetEventMessage()
		{
			return TranslationServiceFactory.GetCurrent().TranslateFormattedArray("You have discovered\nan advanced tribe.");
		}

		public void PostExecute()
		{
			// GameTask is an tightly coupled dependency. No chance of using DI here. Code-Smell.
			// Same for Orders
			GameTask.Enqueue(Orders.NewCity(this.player, X, Y));
		}

		public void PreExecute()
		{
			// No pre-execution logic needed for this event
		}
	}
}