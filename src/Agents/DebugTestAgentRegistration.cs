using System;
using System.Collections.Generic;

namespace CivOne.Agents
{
	public static partial class AgentLoaderEntry
	{
		/// <summary>
		/// This is a temporary debug-only registration for testing the AI selection screen.
		/// </summary>
		private sealed class DebugTestAgentRegistration : IAgentRegistration
		{
			private readonly IAgentInformation _information = new DebugTestAgentInformation();
			private readonly IAgentMemory _memory = new LegacyAgentMemory();
			private readonly ITurnBasedController _controller = new DefaultTurnBasedController();

			public IAgentInformation GetInformation()
			{
				return _information;
			}

			public IAgentMemory GetMemory()
			{
				return _memory;
			}

			public ITurnBasedController GetTurnBasedController()
			{
				return _controller;
			}
		}

		private sealed class DebugTestAgentInformation : IAgentInformation
		{
			public string GetName()
			{
				return "Debug Test AI";
			}

			public string GetAuthor()
			{
				return "CivOne";
			}

			public (int Major, int Minor, int Patch) GetVersion()
			{
				return (0, 1, 0);
			}

			public string GetDescription()
			{
				return "Temporary debug-only AI registration for selection screen tests.";
			}

			public Guid GetUuid()
			{
				return AiDefinitionIds.DebugTest;
			}
		}
	}
}
