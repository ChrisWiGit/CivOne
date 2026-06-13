using CivOne.Services.Random;
using CivOne.Tasks;

namespace CivOne.Units.TribalHuts
{
	internal class HumanTribalHutsVisitorImpl(
		Player player,
		Map map,
		IUnit currentUnit,
		IGame gameInstance,
		ILogger logger,
		IRandomService random) : TribalHutsVisitorImpl(player, map, currentUnit, gameInstance, logger, random)
	{
		protected override void Execute(ITribalHutEventHandler tribalHutEvent)
		{
			tribalHutEvent.PreExecute();

			Message msgBox = Message.General(tribalHutEvent.GetEventMessage());

			msgBox.Done += (_, __) =>
			{
				tribalHutEvent.PostExecute();
			};
			GameTask.Insert(msgBox);
		}
	}
}
