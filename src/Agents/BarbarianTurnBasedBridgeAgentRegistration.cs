using System;
using System.Linq;
using CivOne.Units;

namespace CivOne.Agents
{
	internal sealed class BarbarianTurnBasedBridgeAgentRegistration : IAgentRegistration
	{
		private readonly IAgentInformation _information = new BarbarianTurnBasedBridgeAgentInformation();
		private readonly IAgentMemory _memory = new LegacyAgentMemory();
		private readonly ITurnBasedController _controller = new BarbarianTurnBasedBridgeController();

		public IAgentInformation GetInformation() => _information;

		public IAgentMemory GetMemory() => _memory;

		public ITurnBasedController GetTurnBasedController() => _controller;
	}

	internal sealed class BarbarianTurnBasedBridgeAgentInformation : IAgentInformation
	{
		public string GetName() => "BarbarianTurnBasedBridge";

		public string GetAuthor() => "CivOne";

		public (int Major, int Minor, int Patch) GetVersion() => (1, 0, 0);

		public string GetDescription() => "Delegates barbarian turns to the existing legacy barbarian AI logic.";

		public Guid GetUuid() => AiDefinitionIds.BarbarianBridge;
	}

	internal sealed class BarbarianTurnBasedBridgeController : ITurnBasedController
	{
		public void OnTurn(ITurnSession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			Player player = Game.Instance.CurrentPlayer;
			if (Game.Instance.PlayerNumber(player) != 0)
			{
				session.EndTurn();
				return;
			}

			IPlayerAiController? controller = player.AiController;
			if (controller is null)
			{
				session.EndTurn();
				return;
			}

			byte ownerId = Game.Instance.PlayerNumber(player);
			IUnit[] units = [.. Game.Instance.GetUnits().Where(unit => unit.Owner == ownerId && unit.MovesLeft > 0)];
			foreach (IUnit unit in units)
			{
				controller.Move(unit);
			}

			session.EndTurn();
		}
	}

	internal sealed class BarbarianDisabledAgentRegistration : IAgentRegistration
	{
		private readonly IAgentInformation _information = new BarbarianDisabledAgentInformation();
		private readonly IAgentMemory _memory = new LegacyAgentMemory();
		private readonly ITurnBasedController _controller = new BarbarianDisabledController();

		public IAgentInformation GetInformation() => _information;

		public IAgentMemory GetMemory() => _memory;

		public ITurnBasedController GetTurnBasedController() => _controller;
	}

	internal sealed class BarbarianDisabledAgentInformation : IAgentInformation
	{
		public string GetName() => "BarbarianDisabled";

		public string GetAuthor() => "CivOne";

		public (int Major, int Minor, int Patch) GetVersion() => (1, 0, 0);

		public string GetDescription() => "Disables barbarian AI actions by ending barbarian turns immediately.";

		public Guid GetUuid() => AiDefinitionIds.BarbarianDisabled;
	}

	internal sealed class BarbarianDisabledController : ITurnBasedController
	{
		public void OnTurn(ITurnSession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			Player player = Game.Instance.CurrentPlayer;
			if (Game.Instance.PlayerNumber(player) == 0)
			{
				byte ownerId = Game.Instance.PlayerNumber(player);
				IUnit[] units = [.. Game.Instance.GetUnits().Where(unit => unit.Owner == ownerId && unit.MovesLeft > 0)];
				foreach (IUnit unit in units)
				{
					TurnBasedAgentHost.MarkUnitAsExplicitDisband(unit.Id);
					try
					{
						Game.Instance.DisbandUnit(unit);
					}
					finally
					{
						TurnBasedAgentHost.UnmarkUnitAsExplicitDisband(unit.Id);
					}
				}
			}

			session.EndTurn();
		}
	}
}
