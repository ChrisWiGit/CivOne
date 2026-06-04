using System;

namespace CivOne.Agents
{
	/// <summary>
	/// Minimal host entry for registering external <see cref="IAgentRegistration"/> instances.
	/// This keeps loader integration separate from <see cref="AgentRegistry"/> internals.
	/// </summary>
	public static class AgentLoaderEntry
	{
		/// <summary>
		/// Registers one agent implementation in the runtime registry.
		/// </summary>
		/// <param name="registration">The registration to store.</param>
		public static void Register(IAgentRegistration registration)
		{
			ArgumentNullException.ThrowIfNull(registration);
			AgentRegistry.Instance.Register(registration);
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
	}
}
