using CivOne.Enums;
using CivOne.Services;

namespace CivOne.Units.TribalHuts
{
	public class FriendlyTribeEventHandler : ITribalHutEventHandler
	{
		private readonly int X;
		private readonly int Y;
		private readonly byte Owner;

		public FriendlyTribeEventHandler(int x, int y, byte owner)
		{
			X = x;
			Y = y;
			Owner = owner;
		}

		public string[] GetEventMessage()
		{
			return TranslationServiceFactory.GetCurrent().TranslateFormattedArray("You have discovered\na friendly tribe of\nskilled mercenaries.");
		}

		public void PostExecute()
		{
			Game.Instance.CreateUnit(Common.Random.Next(0, 100) < 50 ?
												UnitType.Cavalry : UnitType.Legion,
												X, Y, Owner, true);
		}

		public void PreExecute()
		{
			// No pre-execution logic needed for this event
		}
	}
}