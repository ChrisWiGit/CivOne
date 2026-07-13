using System;

namespace CivOne.Agents
{
	/// <summary>
	/// Describes the intended strength or maturity level of one AI definition.
	/// </summary>
	public enum AiDifficulty
	{
		/// <summary>
		/// No explicit difficulty classification is assigned.
		/// </summary>
		Unspecified = -1,

		/// <summary>
		/// Chieftain game difficulty.
		/// </summary>
		Chieftain = 0,

		/// <summary>
		/// Warlord game difficulty.
		/// </summary>
		Warlord = 1,

		/// <summary>
		/// Prince game difficulty.
		/// </summary>
		Prince = 2,

		/// <summary>
		/// King game difficulty.
		/// </summary>
		King = 3,

		/// <summary>
		/// Emperor game difficulty.
		/// </summary>
		Emperor = 4,

		/// <summary>
		/// Deity game difficulty.
		/// </summary>
		Deity = 5
	}

	/// <summary>
	/// Provides stable identifiers for built-in AI definitions.
	/// </summary>
	public static class AiDefinitionIds
	{
		/// <summary>
		/// Identifier for the classic built-in legacy AI.
		/// </summary>
		public static readonly Guid Legacy = new("5f3fdc44-02e7-4ddd-9d93-df0f2f57a001");

		/// <summary>
		/// Identifier for the default turn-based command AI.
		/// </summary>
		public static readonly Guid TurnBasedDefault = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

		/// <summary>
		/// Identifier for the adapter that forwards barbarian turns to legacy logic.
		/// </summary>
		public static readonly Guid BarbarianBridge = new("0d6e6f0d-3f88-4b50-8e1a-25a302e4e7c1");

		/// <summary>
		/// Identifier for the agent that disables barbarian actions.
		/// </summary>
		public static readonly Guid BarbarianDisabled = new("5b4d9f8d-9c8b-4c5e-9b1e-7fbf2fce6e70");

		/// <summary>
		/// Identifier for the debug-only test AI registration.
		/// </summary>
		public static readonly Guid DebugTest = new("11111111-2222-3333-4444-555555555555");
	}

	/// <summary>
	/// Describes one AI option that can be shown, selected, or registered at runtime.
	/// </summary>
	/// <param name="id">The stable unique identifier of the AI definition.</param>
	/// <param name="displayName">The user-facing display name.</param>
	/// <param name="description">A short summary of the AI behavior.</param>
	/// <param name="provider">The provider or author name.</param>
	/// <param name="difficulty">The intended difficulty classification.</param>
	public sealed class AiDefinition(
		Guid id,
		string displayName,
		string description,
		string provider,
		AiDifficulty difficulty)
	{
		/// <summary>
		/// Gets the stable unique identifier of the AI definition.
		/// </summary>
		public Guid Id { get; } = id;

		/// <summary>
		/// Gets the user-facing display name.
		/// </summary>
		public string DisplayName { get; } = displayName ?? string.Empty;

		/// <summary>
		/// Gets the short description of the AI behavior.
		/// </summary>
		public string Description { get; } = description ?? string.Empty;

		/// <summary>
		/// Gets the provider or author name.
		/// </summary>
		public string Provider { get; } = provider ?? string.Empty;

		/// <summary>
		/// Gets the intended difficulty classification.
		/// </summary>
		public AiDifficulty Difficulty { get; } = difficulty;
	}

	/// <summary>
	/// Maps <see cref="AiDifficulty"/> classifications to game difficulty indices.
	/// </summary>
	public static class AiDifficultyMapper
	{
		/// <summary>
		/// Converts an AI profile difficulty to a game difficulty index.
		/// </summary>
		/// <param name="difficulty">The profile difficulty classification.</param>
		/// <param name="fallbackIndex">The fallback index to use for <see cref="AiDifficulty.Unspecified"/>.</param>
		/// <returns>The mapped index on the standard game difficulty scale.</returns>
		public static int ToDifficultyIndex(AiDifficulty difficulty, int fallbackIndex)
		{
			if (difficulty == AiDifficulty.Unspecified)
			{
				return fallbackIndex;
			}

			return (int)difficulty;
		}
	}
}