using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Agents;
using Xunit;

namespace CivOne.UnitTests
{
	public class TurnBasedAgentHostIntegrationTests : TestsBase2
	{
		[Fact]
		public void RunForCurrentPlayerIfNeeded_WhenControllerThrows_DoesNotPropagateException()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			ThrowingController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			Exception? actual = Record.Exception(() => TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded());

			// Assert
			Assert.Null(actual);
			Assert.Equal(1, controller.CallCount);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_WhenControllerOmitsEndTurn_CompletesWithoutException()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			NoEndTurnController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			Exception? actual = Record.Exception(() => TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded());

			// Assert
			Assert.Null(actual);
			Assert.Equal(1, controller.CallCount);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_WhenResearchCommandsExceedTypeLimit_ProducesActionTypeLimitError()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			Assert.NotEmpty(aiPlayer.AvailableResearch);

			ResearchSpamController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			bool actual = TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.Contains("ACTION_TYPE_LIMIT_REACHED", controller.ErrorCodes);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_WhenTotalCommandLimitExceeded_ProducesTotalActionLimitError()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			MoveSpamController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			bool actual = TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.Contains("TOTAL_ACTION_LIMIT_REACHED", controller.ErrorCodes);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_DeterministicFullTurn_SetsResearchAndCityProduction()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			City city = Game.Instance.AddCity(aiPlayer, 0, 40, 30);
			Assert.NotNull(city);

			DeterministicFullTurnController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			bool actual = TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.True(controller.EndTurnCalled);
			Assert.True(controller.ResearchSucceeded);
			Assert.True(controller.ProductionSucceeded);

			Assert.NotNull(controller.SelectedResearchName);
			Assert.NotNull(aiPlayer.CurrentResearch);
			Assert.Equal(controller.SelectedResearchName, aiPlayer.CurrentResearch.GetType().Name);

			Assert.NotNull(controller.SelectedProductionName);
			City persistedCity = Game.Instance.GetCities().Single(c => c.Id == city.Id);
			Assert.NotNull(persistedCity.CurrentProduction);
			Assert.Equal(controller.SelectedProductionName, persistedCity.CurrentProduction.GetType().Name);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_LoadsMemoryBeforeControllerExecution()
		{
			// Arrange
			Player aiPlayer = GetHostManagedAiPlayer();
			MemoryProbeAgentMemory memory = new();
			MemoryLoadProbeController controller = new(memory);
			TestAgentRegistration registration = new(controller, memory);
			RegisterAndBind(aiPlayer, registration);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			FakeAgentMemoryStore memoryStore = new();
			memoryStore.StoredByPlayer[aiPlayer.PlayerGuid] = "policy: loaded";
			TurnBasedAgentHost host = new(memoryStore: memoryStore);

			// Act
			bool actual = host.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.True(controller.LoadedValueObserved);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_SavesMemoryAfterControllerExecution()
		{
			// Arrange
			Player aiPlayer = GetHostManagedAiPlayer();
			MemoryProbeAgentMemory memory = new();
			MemorySaveProbeController controller = new(memory);
			TestAgentRegistration registration = new(controller, memory);
			RegisterAndBind(aiPlayer, registration);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			FakeAgentMemoryStore memoryStore = new();
			TurnBasedAgentHost host = new(memoryStore: memoryStore);

			// Act
			bool actual = host.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.Equal("policy: updated", memoryStore.StoredByPlayer[aiPlayer.PlayerGuid]);
			Assert.Equal(registration.GetInformation().GetName(), memoryStore.LastAgentName);
		}

		[Fact]
		public void FileAgentMemoryStore_SaveAndTryLoad_RoundTripsWrapperPayload()
		{
			// Arrange
			using TestTempDirectory temp = new();
			FileAgentMemoryStore store = new(temp.Path);
			Guid playerGuid = Guid.NewGuid();
			IAgentInformation information = new TestAgentInformation();
			const string expectedYaml = "policy: economic";

			// Act
			store.Save(playerGuid, information, gameTurn: 77, expectedYaml);
			bool loaded = store.TryLoad(playerGuid, out string? actualYaml);

			// Assert
			Assert.True(loaded);
			Assert.Equal(expectedYaml, actualYaml);

			string persistedPath = Path.Combine(temp.Path, "ai-memory", $"{playerGuid:N}.yaml");
			string persistedText = File.ReadAllText(persistedPath);
			Assert.Contains("FormatVersion: 1", persistedText);
			Assert.Contains($"PlayerGuid: {playerGuid}", persistedText);
			Assert.Contains($"AgentGuid: {information.GetUuid()}", persistedText);
			Assert.Contains("AgentName:", persistedText);
			Assert.Contains("AgentAuthor:", persistedText);
			Assert.Contains("GameTurn: 77", persistedText);
			Assert.Contains("MemoryYaml:", persistedText);
		}

		[Fact]
		public void FileAgentMemoryStore_TryLoad_WhenFileContainsLegacyRawYaml_ReturnsRawYaml()
		{
			// Arrange
			using TestTempDirectory temp = new();
			FileAgentMemoryStore store = new(temp.Path);
			Guid playerGuid = Guid.NewGuid();
			string memoryDirectory = Path.Combine(temp.Path, "ai-memory");
			Directory.CreateDirectory(memoryDirectory);
			string expectedYaml = "Policy: expansion\nAggressionLevel: 3\n";
			string persistedPath = Path.Combine(memoryDirectory, $"{playerGuid:N}.yaml");
			File.WriteAllText(persistedPath, expectedYaml);

			// Act
			bool loaded = store.TryLoad(playerGuid, out string? actualYaml);

			// Assert
			Assert.True(loaded);
			Assert.Equal(expectedYaml, actualYaml);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_ReportsMetricsToHook()
		{
			// Arrange
			Player aiPlayer = GetHostManagedAiPlayer();
			DeterministicFullTurnController controller = new();
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			FakeAgentTurnMetricsHook metricsHook = new();
			TurnBasedAgentHost host = new(metricsHook: metricsHook);

			// Act
			bool actual = host.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.NotNull(metricsHook.LastMetrics);
			Assert.Equal(aiPlayer.PlayerGuid, metricsHook.LastMetrics!.PlayerGuid);
			Assert.False(metricsHook.LastMetrics.ControllerThrew);
			Assert.False(metricsHook.LastMetrics.UsedHostEndTurnFallback);
			Assert.True(metricsHook.LastMetrics.CommandsIssued >= 1);
			Assert.True(metricsHook.LastMetrics.Duration >= TimeSpan.Zero);
		}

		[Fact]
		public void RunForCurrentPlayerIfNeeded_WhenUnitDisbanded_EmitsUnitDisbandedAndNoUnitDestroyedForSameUnit()
		{
			// Arrange
			ResetHostState();
			Player aiPlayer = GetHostManagedAiPlayer();
			byte aiPlayerNumber = (byte)Game.Instance.PlayerNumber(aiPlayer);
			Guid unitId = Game.Instance.GetUnits().First(unit => unit.Owner == aiPlayerNumber).Id;
			DisbandProbeController controller = new(unitId);
			RegisterAndBind(aiPlayer, controller);
			Game.Instance._currentPlayer = Game.Instance.PlayerNumber(aiPlayer);

			// Act
			bool actual = TurnBasedAgentHost.Instance.RunForCurrentPlayerIfNeeded();

			// Assert
			Assert.True(actual);
			Assert.Contains(controller.Events, e => e.Kind == AgentEventKind.UnitDisbanded && e.EntityId == unitId);
			Assert.DoesNotContain(controller.Events, e => e.Kind == AgentEventKind.UnitDestroyed && e.EntityId == unitId);
		}

		private static Player GetHostManagedAiPlayer()
		{
			Game game = Game.Instance;
			Player? result = game.Players
				.Where(player => player is not null && !player.IsHuman)
				.FirstOrDefault(player => game.PlayerNumber(player) != 0);

			Assert.NotNull(result);
			return result!;
		}

		private static void RegisterAndBind(Player player, ITurnBasedController controller)
		{
			TestAgentRegistration registration = new(controller);
			AgentLoaderEntry.Register(registration);
			AgentLoaderEntry.BindPlayer(player.PlayerGuid, registration.GetInformation().GetUuid());
		}

		private static void RegisterAndBind(Player player, TestAgentRegistration registration)
		{
			AgentLoaderEntry.Register(registration);
			AgentLoaderEntry.BindPlayer(player.PlayerGuid, registration.GetInformation().GetUuid());
		}

		private static void ResetHostState()
		{
			TurnBasedAgentHost host = TurnBasedAgentHost.Instance;
			Type hostType = typeof(TurnBasedAgentHost);

			FieldInfo? lastPlayerGuid = hostType.GetField("_lastPlayerGuid", BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo? lastGameTurn = hostType.GetField("_lastGameTurn", BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo? journals = hostType.GetField("_journals", BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.NotNull(lastPlayerGuid);
			Assert.NotNull(lastGameTurn);
			Assert.NotNull(journals);

			lastPlayerGuid!.SetValue(host, Guid.Empty);
			lastGameTurn!.SetValue(host, int.MinValue);
			((IDictionary)journals!.GetValue(host)!).Clear();
		}

		private sealed class ThrowingController : ITurnBasedController
		{
			public int CallCount { get; private set; }

			public void OnTurn(ITurnSession session)
			{
				CallCount++;
				throw new InvalidOperationException("controller failed");
			}
		}

		private sealed class NoEndTurnController : ITurnBasedController
		{
			public int CallCount { get; private set; }

			public void OnTurn(ITurnSession session)
			{
				CallCount++;
				_ = session.Context.GameTurn;
			}
		}

		private sealed class ResearchSpamController : ITurnBasedController
		{
			public List<string> ErrorCodes { get; } = [];

			public void OnTurn(ITurnSession session)
			{
				if (session.Context.AvailableResearchNames.Count == 0)
				{
					session.EndTurn();
					return;
				}

				string researchName = session.Context.AvailableResearchNames[0];
				for (int i = 0; i < 25; i++)
				{
					ICommandResult result = session.Commands.Research.ChooseResearch(researchName);
					if (!result.Success)
					{
						ErrorCodes.Add(result.ErrorCode);
					}
				}
			}
		}

		private sealed class MoveSpamController : ITurnBasedController
		{
			public List<string> ErrorCodes { get; } = [];

			public void OnTurn(ITurnSession session)
			{
				for (int i = 0; i < 120; i++)
				{
					ICommandResult result = session.Commands.Units.Move(Guid.Empty, 0, 0);
					if (!result.Success)
					{
						ErrorCodes.Add(result.ErrorCode);
					}
				}
			}
		}

		private sealed class DeterministicFullTurnController : ITurnBasedController
		{
			public bool EndTurnCalled { get; private set; }
			public bool ResearchSucceeded { get; private set; }
			public bool ProductionSucceeded { get; private set; }
			public string? SelectedResearchName { get; private set; }
			public string? SelectedProductionName { get; private set; }

			public void OnTurn(ITurnSession session)
			{
				if (session.Context.AvailableResearchNames.Count > 0)
				{
					SelectedResearchName = session.Context.AvailableResearchNames[0];
					ICommandResult researchResult = session.Commands.Research.ChooseResearch(SelectedResearchName);
					ResearchSucceeded = researchResult.Success;
				}

				ICityView? city = session.Context.OwnCities.FirstOrDefault();
				if (city is not null && city.AvailableProductionNames.Count > 0)
				{
					SelectedProductionName = city.AvailableProductionNames[0];
					ICommandResult productionResult = session.Commands.Cities.ChooseProduction(city.Id, SelectedProductionName);
					ProductionSucceeded = productionResult.Success;
				}

				session.EndTurn();
				EndTurnCalled = true;
			}
		}

		private sealed class DisbandProbeController(Guid unitId) : ITurnBasedController
		{
			private readonly Guid _unitId = unitId;

			public List<AgentEvent> Events { get; } = [];

			public void OnTurn(ITurnSession session)
			{
				ICommandResult result = session.Commands.Units.Disband(_unitId);
				Assert.True(result.Success);

				EventReadResult eventsResult = session.Events.ReadSince(0);
				Events.AddRange(eventsResult.Events);

				session.EndTurn();
			}
		}

		private sealed class MemoryLoadProbeController(MemoryProbeAgentMemory memory) : ITurnBasedController
		{
			public bool LoadedValueObserved { get; private set; }

			public void OnTurn(ITurnSession session)
			{
				LoadedValueObserved = string.Equals(memory.GetMemory(), "policy: loaded", StringComparison.Ordinal);
				session.EndTurn();
			}
		}

		private sealed class MemorySaveProbeController(MemoryProbeAgentMemory memory) : ITurnBasedController
		{
			public void OnTurn(ITurnSession session)
			{
				memory.SetMemory("policy: updated");
				session.EndTurn();
			}
		}

		private sealed class TestAgentRegistration : IAgentRegistration
		{
			private readonly IAgentInformation _information = new TestAgentInformation();
			private readonly IAgentMemory _memory;
			private readonly ITurnBasedController _controller;

			public TestAgentRegistration(ITurnBasedController controller)
			{
				_memory = new TestAgentMemory();
				_controller = controller;
			}

			public TestAgentRegistration(ITurnBasedController controller, IAgentMemory memory)
			{
				_memory = memory;
				_controller = controller;
			}

			public IAgentInformation GetInformation() => _information;
			public IAgentMemory GetMemory() => _memory;
			public ITurnBasedController GetTurnBasedController() => _controller;
		}

		private sealed class TestAgentInformation : IAgentInformation
		{
			private readonly Guid _uuid = Guid.NewGuid();

			public string GetName() => nameof(TestAgentInformation);
			public string GetAuthor() => nameof(TurnBasedAgentHostIntegrationTests);
			public (int Major, int Minor, int Patch) GetVersion() => (1, 0, 0);
			public string GetDescription() => "Test registration for TurnBasedAgentHost integration tests.";
			public Guid GetUuid() => _uuid;
		}

		private sealed class TestAgentMemory : IAgentMemory
		{
			private string _yaml = string.Empty;

			public void SetMemory(string yaml)
			{
				_yaml = yaml ?? string.Empty;
			}

			public string GetMemory() => _yaml;
		}

		private sealed class MemoryProbeAgentMemory : IAgentMemory
		{
			private string _yaml = string.Empty;

			public void SetMemory(string yaml)
			{
				_yaml = yaml ?? string.Empty;
			}

			public string GetMemory() => _yaml;
		}

		private sealed class FakeAgentMemoryStore : IAgentMemoryStore
		{
			public Dictionary<Guid, string> StoredByPlayer { get; } = [];
			public string LastAgentName { get; private set; } = string.Empty;

			public bool TryLoad(Guid playerGuid, out string? yaml)
			{
				if (StoredByPlayer.TryGetValue(playerGuid, out string? value))
				{
					yaml = value;
					return true;
				}

				yaml = null;
				return false;
			}

			public void Save(Guid playerGuid, IAgentInformation information, int gameTurn, string yaml)
			{
				LastAgentName = information.GetName();
				Assert.True(gameTurn >= 0);
				Assert.NotEqual(Guid.Empty, information.GetUuid());
				Assert.False(string.IsNullOrWhiteSpace(information.GetAuthor()));
				Assert.False(string.IsNullOrWhiteSpace(information.GetName()));
				Assert.True(information.GetVersion().Major >= 0);
				Assert.False(string.IsNullOrWhiteSpace(information.GetDescription()));

				StoredByPlayer[playerGuid] = yaml;
			}
		}

		private sealed class FakeAgentTurnMetricsHook : IAgentTurnMetricsHook
		{
			public AgentTurnMetrics? LastMetrics { get; private set; }

			public void OnTurnCompleted(AgentTurnMetrics metrics)
			{
				LastMetrics = metrics;
			}
		}

		private sealed class TestTempDirectory : IDisposable
		{
			public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));

			public TestTempDirectory()
			{
				Directory.CreateDirectory(Path);
			}

			public void Dispose()
			{
				if (Directory.Exists(Path))
				{
					Directory.Delete(Path, true);
				}
			}
		}
	}
}
