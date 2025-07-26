namespace CivOne.Units.TribalHuts
{
	internal interface ITribalHutEventHandler
	{
		void PreExecute();
		void PostExecute();

		string[] GetEventMessage();
	}
}