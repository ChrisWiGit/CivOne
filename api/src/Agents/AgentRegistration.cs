using System;

namespace CivOne.Agents
{
	/// <summary>
	/// Describes one registered agent implementation metadata used by
	/// <see cref="IAgentRegistration"/> and host binding.
	/// </summary>
	public interface IAgentInformation
	{
		/// <summary>
		/// Gets the display name of the agent.
		/// </summary>
		/// <returns>The agent name.</returns>
		string GetName();

		/// <summary>
		/// Gets the author name of the agent.
		/// </summary>
		/// <returns>The author name.</returns>
		string GetAuthor();

		/// <summary>
		/// Gets the semantic version of the agent.
		/// </summary>
		/// <returns>The version tuple.</returns>
		(int Major, int Minor, int Patch) GetVersion();

		/// <summary>
		/// Gets the human-readable description of the agent.
		/// </summary>
		/// <returns>The description text.</returns>
		string GetDescription();

		/// <summary>
		/// Gets the stable unique identifier of the agent.
		/// </summary>
		/// <returns>The agent identifier.</returns>
		Guid GetUuid();
	}

	/// <summary>
	/// Provides save/load memory exchange for an agent.
	/// Host calls this contract at save/load boundaries while turn execution uses
	/// <see cref="ITurnBasedController"/>.
	/// </summary>
	public interface IAgentMemory
	{
		/// <summary>
		/// Sets the serialized memory content for the agent.
		/// </summary>
		/// <param name="yaml">The serialized YAML content.</param>
		void SetMemory(string yaml);

		/// <summary>
		/// Gets the serialized memory content for the agent.
		/// </summary>
		/// <returns>The serialized YAML content.</returns>
		string GetMemory();
	}

	/// <summary>
	/// Registers one agent with the host by combining
	/// <see cref="IAgentInformation"/>, <see cref="IAgentMemory"/>, and
	/// <see cref="ITurnBasedController"/>.
	/// </summary>
	public interface IAgentRegistration
	{
		/// <summary>
		/// Gets metadata for the registered agent.
		/// </summary>
		/// <returns>The agent information provider.</returns>
		IAgentInformation GetInformation();

		/// <summary>
		/// Gets the memory bridge for the registered agent.
		/// </summary>
		/// <returns>The agent memory provider.</returns>
		IAgentMemory GetMemory();

		/// <summary>
		/// Gets the turn-based controller implementation.
		/// </summary>
		/// <returns>The turn-based controller.</returns>
		ITurnBasedController GetTurnBasedController();
	}
}