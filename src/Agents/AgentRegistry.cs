using System;
using System.Collections.Generic;

namespace CivOne.Agents
{
	/// <summary>
	/// Internal host registry for <see cref="IAgentRegistration"/> instances.
	/// This is runtime infrastructure and not part of the public API contract.
	/// </summary>
	internal sealed class AgentRegistry
	{
		private readonly Dictionary<Guid, IAgentRegistration> _agentsById = [];
		private readonly Dictionary<Guid, Guid> _playerToAgent = [];

		/// <summary>
		/// Gets singleton registry instance.
		/// </summary>
		public static AgentRegistry Instance { get; } = new();

		/// <summary>
		/// Registers or replaces one agent implementation by its stable UUID.
		/// </summary>
		/// <param name="registration">The registration object to store.</param>
		public void Register(IAgentRegistration registration)
		{
			ArgumentNullException.ThrowIfNull(registration);
			Guid agentId = registration.GetInformation().GetUuid();
			_agentsById[agentId] = registration;
		}

		/// <summary>
		/// Binds one runtime player to one registered agent UUID.
		/// </summary>
		/// <param name="playerGuid">The runtime player identifier.</param>
		/// <param name="agentGuid">The registered agent identifier.</param>
		public void BindPlayer(Guid playerGuid, Guid agentGuid)
		{
			_playerToAgent[playerGuid] = agentGuid;
		}

		/// <summary>
		/// Resolves the effective registration for one runtime player.
		/// </summary>
		/// <param name="playerGuid">The runtime player identifier.</param>
		/// <param name="registration">The resolved registration when found.</param>
		/// <returns>
		/// <see langword="true"/> when a matching registration exists;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool TryResolve(Guid playerGuid, out IAgentRegistration? registration)
		{
			if (_playerToAgent.TryGetValue(playerGuid, out Guid boundAgentId)
				&& _agentsById.TryGetValue(boundAgentId, out registration))
			{
				return true;
			}

			if (_agentsById.TryGetValue(playerGuid, out registration))
			{
				return true;
			}

			registration = null;
			return false;
		}
	}
}
