using CivOne.Enums;
using CivOne.Services;
using CivOne.Services.Random;

namespace CivOne.Units.TribalHuts
{
	public class FriendlyTribeEventHandler(int x, int y, byte owner) : ITribalHutEventHandler
	{
		private readonly int X = x;
		private readonly int Y = y;
		private readonly byte Owner = owner;

		public string[] GetEventMessage()
		{
			return TranslationServiceFactory.GetCurrent().TranslateFormattedArray("You have discovered\na friendly tribe of\nskilled mercenaries.");
		}

		public void PostExecute()
		{
			Game.Instance.CreateUnit(RandomServiceFactory.Create().Hit(50) ?
										UnitType.Cavalry : UnitType.Legion,
										X, Y, Owner, true);
		}

		public void PreExecute()
		{
			// No pre-execution logic needed for this event
		}
	}
}