using System;
using System.Diagnostics;

namespace CivOne.Units.TribalHuts
{
	internal class TribalHutsVisitorImpl : ITribalHutsVisitor
	{
		private readonly Map map;
		private readonly IUnit currentUnit;
		private readonly IGame gameInstance;
		private readonly Player player;
		private readonly ILogger logger;
		private readonly Random random;

		private readonly Action[] events;

		public TribalHutsVisitorImpl(
			Player player,
			Map map,
			IUnit currentUnit,
			IGame gameInstance,
			ILogger logger,
			Random random)

		{
			this.player = player;
			this.map = map;
			this.currentUnit = currentUnit;
			this.gameInstance = gameInstance;
			this.logger = logger;
			this.random = random;

			events =
			[
				FriendlyEvent,
				AncientScrollsEvent,
				BarbariansEvent,
				MetalDepositsEvent
			];
		}


		public void ExecuteRandomTribalHutEvent()
		{
			int eventIndex = random.Next(0, events.Length);

			Debug.Assert(eventIndex >= 0 && eventIndex < events.Length, "Invalid event index");

			events[eventIndex]();
		}

		protected virtual void Execute(ITribalHutEventHandler tribalHutEvent)
		{
			tribalHutEvent.PreExecute();
			tribalHutEvent.PostExecute();
		}

		private ITribalHutEventHandler CreateFriendlyTribeEvent()
		{
			return new FriendlyTribeEventHandler(
				currentUnit.X, currentUnit.Y, currentUnit.Owner);
		}

		private ITribalHutEventHandler CreateAncientScrollsEvent()
		{
			return new AncientScrollsEventHandler(
				currentUnit.X, currentUnit.Y, player, logger);
		}

		private ITribalHutEventHandler CreateBarbariansEvent()
		{
			return new BarbariansEventHandler(
				currentUnit.X, currentUnit.Y, gameInstance, map, random);
		}

		private ITribalHutEventHandler CreateMetalDepositsEvent()
		{
			return new MetalDepositsEventHandler(player);
		}

		private ITribalHutEventHandler CreateAdvancedTribeEvent()
		{
			return new AdvancedTribeEventHandler(currentUnit.X, currentUnit.Y, player);
		}

		private void FriendlyEvent()
		{
			bool hasCityNearby = currentUnit.NearestCity <= 3;

			if (hasCityNearby)
			{
				Execute(CreateFriendlyTribeEvent());
				return;
			}

			bool hasHighLandValue = map[currentUnit.X, currentUnit.Y].LandValue > 12;
			if (hasHighLandValue)
			{
				Execute(CreateAdvancedTribeEvent());
				return;
			}

			Execute(CreateMetalDepositsEvent());
		}

		private void AncientScrollsEvent()
		{
			bool isFirstRound = gameInstance.GameTurn == 0;
			bool isLateGame = Common.TurnToYear(gameInstance.GameTurn) >= 1000;

			if (isFirstRound || isLateGame)
			{
				// obviously someone doesn't want to have ancient scrolls in the very first round
				// this happens only if we go to far with a settler (who does this?)
				// It may even happen to found a city in the first round in this way!
				Execute(CreateMetalDepositsEvent());
				return;
			}

			Execute(CreateAncientScrollsEvent());
		}

		private void MetalDepositsEvent()
		{
			Execute(CreateMetalDepositsEvent());
		}

		private void BarbariansEvent()
		{
			// CW: original code was
			//	if (NearestCity < 4 || !Game.Instance.GetCities().Any(c => Player == c.Owner))
			// the second part makes no sense
			// it just checks if the player has any cities at all, so it is always true 
			// Otherwise the player would be not playing
			// So it results always to false.
			// I think the intention was to check if the nearest player city is there.
			// But not sure.
			bool hasCityNearby = currentUnit.NearestCity < 4;

			if (hasCityNearby)
			{
				Execute(CreateFriendlyTribeEvent());
				return;
			}

			Execute(CreateBarbariansEvent());
		}

	}
}