using System;
using System.Collections.Generic;
using System.Linq;

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
		private readonly Dictionary<Guid, AiDefinition> _definitionsById = [];
		private readonly Dictionary<Guid, Func<IAgentRegistration>> _factoriesById = [];

		/// <summary>
		/// Gets singleton registry instance.
		/// </summary>
		public static AgentRegistry Instance { get; } = new();

		/// <summary>
		/// Registers or replaces one agent implementation by its stable UUID.
		/// </summary>
		/// <param name="registration">The registration object to store.</param>
		/// <param name="difficulty">The difficulty of the agent.</param>
		public void Register(
			IAgentRegistration registration,
			AiDifficulty difficulty = AiDifficulty.Unspecified)
		{
			ArgumentNullException.ThrowIfNull(registration);
			Guid agentId = registration.GetInformation().GetUuid();
			_agentsById[agentId] = registration;
			_factoriesById.Remove(agentId);

			IAgentInformation information = registration.GetInformation();
			_definitionsById[agentId] = new AiDefinition(
				agentId,
				information.GetName(),
				information.GetDescription(),
				information.GetAuthor(),
				difficulty);
		}

		/// <summary>
		/// Registers one agent implementation without creating it yet.
		/// The definition is enough to show the agent in the selection menu; the factory runs only
		/// when a player is actually resolved to this agent.
		/// </summary>
		/// <param name="agentId">
		/// The stable UUID the agent is registered and persisted under.
		/// </param>
		/// <param name="definition">
		/// The display metadata shown in the selection menu.
		/// </param>
		/// <param name="factory">
		/// Creates the registration on first use.
		/// </param>
		public void RegisterLazy(Guid agentId, AiDefinition definition, Func<IAgentRegistration> factory)
		{
			ArgumentNullException.ThrowIfNull(definition);
			ArgumentNullException.ThrowIfNull(factory);

			_agentsById.Remove(agentId);
			_factoriesById[agentId] = factory;
			_definitionsById[agentId] = definition;
		}

		/// <summary>
		/// Removes one agent registration.
		/// </summary>
		/// <param name="agentId">
		/// The UUID of the agent to remove.
		/// </param>
		public void Unregister(Guid agentId)
		{
			_agentsById.Remove(agentId);
			_factoriesById.Remove(agentId);
			_definitionsById.Remove(agentId);

			foreach (Guid playerGuid in _playerToAgent.Where(x => x.Value == agentId).Select(x => x.Key).ToArray())
			{
				_playerToAgent.Remove(playerGuid);
			}
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
				&& TryMaterialize(boundAgentId, out registration))
			{
				return true;
			}

			return TryMaterialize(playerGuid, out registration);
		}

		public bool TryResolveAi(Guid aiId, out IAgentRegistration? registration)
		{
			if (aiId != Guid.Empty)
			{
				return TryMaterialize(aiId, out registration);
			}

			registration = null;
			return false;
		}

		public IReadOnlyCollection<AiDefinition> GetRegisteredDefinitions()
		{
			return [.. _definitionsById.Values];
		}

		/// <summary>
		/// Resolves an agent, creating it from its registered factory on first access.
		/// </summary>
		/// <param name="agentId">
		/// The agent UUID to resolve.
		/// </param>
		/// <param name="registration">
		/// The resolved registration when found.
		/// </param>
		/// <returns>
		/// True when a registration exists or could be created.
		/// </returns>
		private bool TryMaterialize(Guid agentId, out IAgentRegistration? registration)
		{
			if (_agentsById.TryGetValue(agentId, out registration))
			{
				return true;
			}

			if (!_factoriesById.TryGetValue(agentId, out Func<IAgentRegistration>? factory))
			{
				registration = null;
				return false;
			}

			registration = factory();
			if (registration == null)
			{
				return false;
			}

			_agentsById[agentId] = registration;
			return true;
		}
	}
}
