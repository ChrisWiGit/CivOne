using CivOne.Tasks;

namespace CivOne.Units.TribalHuts
{
	internal class HumanTribalHutsVisitorImpl(
		Player player,
		Map map,
		IUnit currentUnit,
		IGame gameInstance,
		ILoggerService logger,
		Random random) : TribalHutsVisitorImpl(player, map, currentUnit, gameInstance, logger, random)
	{
		protected override void Execute(ITribalHutEventHandler tribalHutEvent)
		{
			tribalHutEvent.PreExecute();

			Message msgBox = Message.General(tribalHutEvent.GetEventMessage());

			msgBox.Done += (sender, e) =>
			{
				tribalHutEvent.PostExecute();
			};
			GameTask.Insert(msgBox);
		}
	}
}
