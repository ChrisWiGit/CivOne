using System;
using System.Collections.Generic;
using CivOne.Agents;
using CivOne.Services;

namespace CivOne.TestPlugin
{
	/// <summary>
	/// Offers a single AI variant and counts how often the host actually creates it.
	/// The counter is read back by the tests through reflection to prove that listing the AI in the
	/// selection menu does not construct it.
	/// </summary>
	public sealed class TestAiProvider : IPluginAiProvider
	{
		/// <summary>
		/// The stable identifier of the offered AI variant.
		/// </summary>
		public static readonly Guid TestAiId = new("a1b2c3d4-0000-4000-8000-0123456789ab");

		/// <summary>
		/// How often CreateAi has been called since the assembly was loaded.
		/// </summary>
		public static int CreateAiCallCount;

		/// <summary>
		/// How often GetAiDescriptors has been called since the assembly was loaded.
		/// </summary>
		public static int DescriptorCallCount;

		/// <summary>
		/// The difficulty seen during the last turn, to prove it arrives per player.
		/// </summary>
		public static AiDifficulty LastObservedDifficulty = AiDifficulty.Unspecified;

		/// <inheritdoc />
		public IReadOnlyList<AiDescriptor> GetAiDescriptors(ITranslationService translationService)
		{
			DescriptorCallCount++;
			return
			[
				new AiDescriptor(
					TestAiId,
					"Test AI",
					"CivOne Tests",
					"An AI variant that exists only for the plugin tests.",
					"1.0.0",
					null,
					AiDifficulty.Prince)
			];
		}

		/// <inheritdoc />
		public IAgentRegistration CreateAi(Guid id, AiCreationContext context)
		{
			CreateAiCallCount++;
			return new TestAgentRegistration(id);
		}

		private sealed class TestAgentRegistration(Guid id) : IAgentRegistration
		{
			private readonly TestAgentInformation _information = new(id);
			private readonly TestAgentMemory _memory = new();
			private readonly TestTurnBasedController _controller = new();

			public IAgentInformation GetInformation() => _information;

			public IAgentMemory GetMemory() => _memory;

			public ITurnBasedController GetTurnBasedController() => _controller;
		}

		private sealed class TestAgentInformation(Guid id) : IAgentInformation
		{
			public string GetName() => "Test AI";

			public string GetAuthor() => "CivOne Tests";

			public (int Major, int Minor, int Patch) GetVersion() => (1, 0, 0);

			public string GetDescription() => "An AI variant that exists only for the plugin tests.";

			public Guid GetUuid() => id;
		}

		private sealed class TestAgentMemory : IAgentMemory
		{
			private string _memory = string.Empty;

			public void SetMemory(string yaml) => _memory = yaml ?? string.Empty;

			public string GetMemory() => _memory;
		}

		private sealed class TestTurnBasedController : ITurnBasedController
		{
			public void OnTurn(ITurnSession session)
			{
				if (session == null) return;

				// The difficulty is per player, so it is read here rather than at creation time.
				LastObservedDifficulty = session.Context.Difficulty;
				session.EndTurn();
			}
		}
	}
}
