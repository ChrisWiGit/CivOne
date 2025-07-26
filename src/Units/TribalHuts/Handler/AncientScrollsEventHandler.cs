
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tasks;

namespace CivOne.Units.TribalHuts
{
	public class AncientScrollsEventHandler : ITribalHutEventHandler
	{
		private readonly int x;
		private readonly int y;

		private readonly ILogger logger;


		private readonly Player player;

		public AncientScrollsEventHandler(int x, int y, Player player, ILogger logger)
		{
			this.logger = logger;
			this.player = player;
			this.x = x;
			this.y = y;
		}

		public string[] GetEventMessage()
		{
			return
			[
				"You have discovered",
				"scrolls of ancient wisdom."
			];
		}

		public void PreExecute()
		{
			// No pre-execution logic needed for this event
		}

		public void PostExecute()
		{
			// 
			int MAX_ADVANCES = AdvanceExtensions.AllAdvances.Length;

			var available = player.AvailableResearch;
			int advanceId = Common.Random.Next(0, MAX_ADVANCES);

			// This works because advances are sorted from early to modern technologies.
			// So we start with i the lowest advance and try to find the first next lowest advance that is available.
			for (int i = 0; i < 1000; i++)
			{
				int targetId = (advanceId + i) % MAX_ADVANCES;
				IAdvance foundAdvance = available.FirstOrDefault(a => a.Id == targetId);

				if (foundAdvance != null)
				{
					logger.Log("Found advance: {0} (ID: {1}) in {2} attempts with starting ID: {3}",
						foundAdvance.Name, foundAdvance.Id, i, advanceId);
					GameTask.Enqueue(new GetAdvance(player, foundAdvance));
					break;
				}
			}

		}
	}
}