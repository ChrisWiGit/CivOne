using System;
using System.Collections.Generic;
using CivOne.Services;

namespace CivOne.Agents
{
	/// <summary>
	/// Minimal host entry for registering external <see cref="IAgentRegistration"/> instances.
	/// This keeps loader integration separate from <see cref="AgentRegistry"/> internals.
	/// </summary>
	public static partial class AgentLoaderEntry
	{
		static AgentLoaderEntry()
		{
			Register(
				new BarbarianTurnBasedBridgeAgentRegistration(),
				AiDifficulty.Prince);

			Register(
				new BarbarianDisabledAgentRegistration(),
				AiDifficulty.Unspecified);

#if DEBUG
			Register(
				new DebugTestAgentRegistration(),
				AiDifficulty.Deity);
#endif
		}

		/// <summary>
		/// Registers one agent implementation in the runtime registry.
		/// </summary>
		/// <param name="registration">The registration to store.</param>
		/// <param name="difficulty">The difficulty of the agent.</param>
		public static void Register(
			IAgentRegistration registration,
			AiDifficulty difficulty = AiDifficulty.Unspecified)
		{
			ArgumentNullException.ThrowIfNull(registration);
			AgentRegistry.Instance.Register(registration, difficulty);
		}

		/// <summary>
		/// Binds one runtime player to a registered agent UUID.
		/// </summary>
		/// <param name="playerGuid">The runtime player identifier.</param>
		/// <param name="agentGuid">The target agent identifier.</param>
		public static void BindPlayer(Guid playerGuid, Guid agentGuid)
		{
			AgentRegistry.Instance.BindPlayer(playerGuid, agentGuid);
		}

		public static IReadOnlyCollection<AiDefinition> GetAvailableDefinitions()
		{
			var translate = TranslationServiceFactory.CreateDefault();
				
			List<AiDefinition> result =
			[
				new AiDefinition(AiDefinitionIds.Legacy, translate.Translate("Legacy AI"), translate.Translate("Classic built-in AI path."), "CivOne", AiDifficulty.Prince),
				new AiDefinition(AiDefinitionIds.TurnBasedDefault, translate.Translate("Turn-Based Default"), translate.Translate("Default command-based turn controller."), "CivOne", AiDifficulty.Prince)
			];

			result.AddRange(AgentRegistry.Instance.GetRegisteredDefinitions());
			return result;
		}
	}
}
