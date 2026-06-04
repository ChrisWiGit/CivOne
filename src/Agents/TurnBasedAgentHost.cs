using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Persistence.Game;
using CivOne.Persistence.Yaml;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Agents
{
	/// <summary>
	/// Hosts one turn-based agent execution cycle and connects
	/// <see cref="ITurnBasedController"/>, <see cref="ITurnSession"/>,
	/// and runtime turn flow in <see cref="Tasks.Turn"/>.
	/// </summary>
	internal sealed class TurnBasedAgentHost : BaseInstance
	{
		private static readonly object _unitRemovalEventLock = new();
		private static readonly HashSet<Guid> _explicitDisbandUnitIds = [];
		private static RingEventJournal? _currentUnitRemovalJournal;

		private readonly IAgentBindingResolver _resolver;
		private readonly IAgentMemoryStore _memoryStore;
		private readonly IAgentTurnMetricsHook _metricsHook;
		private readonly Dictionary<Guid, RingEventJournal> _journals = [];
		private Guid _lastPlayerGuid = Guid.Empty;
		private int _lastGameTurn = int.MinValue;

		public static TurnBasedAgentHost Instance { get; } = new();

		internal TurnBasedAgentHost(
			IAgentBindingResolver? resolver = null,
			IAgentMemoryStore? memoryStore = null,
			IAgentTurnMetricsHook? metricsHook = null)
		{
			_resolver = resolver ?? new AgentBindingResolver();
			_memoryStore = memoryStore ?? new FileAgentMemoryStore();
			_metricsHook = metricsHook ?? new NoOpAgentTurnMetricsHook();
		}

		internal static bool ShouldHandlePlayer(Player? player)
		{
			if (player is null || player.IsHuman)
			{
				return false;
			}

			// Barbarian flow stays on legacy path for now.
			return Game.Instance.PlayerNumber(player) != 0;
		}

		internal bool RunForCurrentPlayerIfNeeded()
		{
			Player player = Game.CurrentPlayer;
			if (!ShouldHandlePlayer(player))
			{
				return false;
			}

			if (_lastPlayerGuid == player.PlayerGuid && _lastGameTurn == Game.GameTurn)
			{
				return true;
			}

			IAgentRegistration registration = _resolver.Resolve(player);
			IAgentInformation information = registration.GetInformation();
			LoadMemory(player.PlayerGuid, registration);
			RingEventJournal journal = GetJournal(player.PlayerGuid);
			journal.Append(AgentEventKind.TurnStarted, player.PlayerGuid, null, "Turn started.");

			TurnSession session = new(player, journal);
			Stopwatch durationWatch = Stopwatch.StartNew();
			bool controllerThrew = false;
			bool hostEndedTurn = false;
			BeginUnitRemovalCapture(journal);
			try
			{
				registration.GetTurnBasedController().OnTurn(session);
			}
			catch (Exception ex)
			{
				controllerThrew = true;
				Log("Turn-based agent failed for {0}: {1}", player.TribeNamePlural, ex.Message);
				try
				{
					string memory = registration.GetMemory().GetMemory();
					Log("Turn-based agent memory dump ({0}): {1}", player.TribeNamePlural, memory);
				}
				catch (Exception memoryEx)
				{
					Log("Turn-based agent memory dump failed for {0}: {1}", player.TribeNamePlural, memoryEx.Message);
				}
			}
			finally
			{
				EndUnitRemovalCapture();
			}

			if (!session.IsTurnEnded)
			{
				session.EndTurn();
				hostEndedTurn = true;
				Log("Turn-based agent did not call EndTurn(); host ended turn automatically for {0}.", player.TribeNamePlural);
			}

			SaveMemory(player.PlayerGuid, registration);
			durationWatch.Stop();

			_metricsHook.OnTurnCompleted(new AgentTurnMetrics(
				playerGuid: player.PlayerGuid,
				agentGuid: information.GetUuid(),
				agentName: information.GetName(),
				gameTurn: Game.GameTurn,
				commandsIssued: session.TotalCommandCount,
				failedCommands: session.FailedCommandCount,
				duration: durationWatch.Elapsed,
				usedHostEndTurnFallback: hostEndedTurn,
				controllerThrew: controllerThrew));

			_lastPlayerGuid = player.PlayerGuid;
			_lastGameTurn = Game.GameTurn;
			return true;
		}

		private static void BeginUnitRemovalCapture(RingEventJournal journal)
		{
			ArgumentNullException.ThrowIfNull(journal);

			lock (_unitRemovalEventLock)
			{
				_currentUnitRemovalJournal = journal;
			}
		}

		private static void EndUnitRemovalCapture()
		{
			lock (_unitRemovalEventLock)
			{
				_currentUnitRemovalJournal = null;
				_explicitDisbandUnitIds.Clear();
			}
		}

		internal static void MarkUnitAsExplicitDisband(Guid unitId)
		{
			lock (_unitRemovalEventLock)
			{
				_explicitDisbandUnitIds.Add(unitId);
			}
		}

		internal static void UnmarkUnitAsExplicitDisband(Guid unitId)
		{
			lock (_unitRemovalEventLock)
			{
				_explicitDisbandUnitIds.Remove(unitId);
			}
		}

		internal static void NotifyUnitRemoved(IUnit? unit)
		{
			if (unit is null)
			{
				return;
			}

			lock (_unitRemovalEventLock)
			{
				if (_currentUnitRemovalJournal is null)
				{
					return;
				}

				bool explicitDisband = _explicitDisbandUnitIds.Remove(unit.Id);
				AgentEventKind kind = explicitDisband ? AgentEventKind.UnitDisbanded : AgentEventKind.UnitDestroyed;
				string message = explicitDisband ? "Unit disbanded." : "Unit destroyed.";
				_currentUnitRemovalJournal.Append(kind, unit.Id, unit.GetType().Name, message);
			}
		}

		private RingEventJournal GetJournal(Guid playerGuid)
		{
			if (!_journals.TryGetValue(playerGuid, out RingEventJournal? journal))
			{
				journal = new RingEventJournal();
				_journals[playerGuid] = journal;
			}

			return journal;
		}

		private void LoadMemory(Guid playerGuid, IAgentRegistration registration)
		{
			try
			{
				if (_memoryStore.TryLoad(playerGuid, out string? yaml))
				{
					registration.GetMemory().SetMemory(yaml ?? string.Empty);
				}
			}
			catch (Exception ex)
			{
				Log("Turn-based agent memory load failed for player {0}: {1}", playerGuid, ex.Message);
			}
		}

		private void SaveMemory(Guid playerGuid, IAgentRegistration registration)
		{
			try
			{
				string yaml = registration.GetMemory().GetMemory();
				_memoryStore.Save(playerGuid, registration.GetInformation(), Game.GameTurn, yaml);
			}
			catch (Exception ex)
			{
				Log("Turn-based agent memory save failed for player {0}: {1}", playerGuid, ex.Message);
			}
		}
	}

	internal interface IAgentMemoryStore
	{
		bool TryLoad(Guid playerGuid, out string? yaml);

		void Save(Guid playerGuid, IAgentInformation information, int gameTurn, string yaml);
	}

	internal sealed class FileAgentMemoryStore : BaseInstance, IAgentMemoryStore
	{
		private const string MemoryDirectoryName = "ai-memory";
		private const int CurrentFormatVersion = 1;
		private readonly string _storageDirectory;

		internal FileAgentMemoryStore(string? storageDirectory = null)
		{
			_storageDirectory = storageDirectory ?? Runtime.StorageDirectory;
		}

		public bool TryLoad(Guid playerGuid, out string? yaml)
		{
			string path = GetPath(playerGuid);
			if (!File.Exists(path))
			{
				yaml = null;
				return false;
			}

			string persisted = File.ReadAllText(path);
			try
			{
				AgentMemoryFileDto dto = YamlReader.OfString(persisted).WithStandard().As<AgentMemoryFileDto>();
				if (dto is not null && !string.IsNullOrWhiteSpace(dto.MemoryYaml))
				{
					yaml = dto.MemoryYaml;
					return true;
				}
			}
			catch
			{
				// Backward compatible fallback: previous versions stored raw memory YAML directly.
			}

			yaml = persisted;
			return true;
		}

		public void Save(Guid playerGuid, IAgentInformation information, int gameTurn, string yaml)
		{
			string directory = Path.Combine(_storageDirectory, MemoryDirectoryName);
			Directory.CreateDirectory(directory);

			AgentMemoryFileDto dto = new()
			{
				FormatVersion = CurrentFormatVersion,
				SavedAtUtc = DateTime.UtcNow,
				PlayerGuid = playerGuid,
				AgentGuid = information.GetUuid(),
				AgentName = information.GetName(),
				AgentAuthor = information.GetAuthor(),
				GameTurn = gameTurn,
				MemoryYaml = yaml ?? string.Empty
			};

			string persisted = YamlWriter.Of(dto).WithStandard().AsString();
			File.WriteAllText(GetPath(playerGuid), persisted);
		}

		private string GetPath(Guid playerGuid)
		{
			return Path.Combine(_storageDirectory, MemoryDirectoryName, $"{playerGuid:N}.yaml");
		}

		private sealed class AgentMemoryFileDto
		{
			public int FormatVersion { get; set; }

			public DateTime SavedAtUtc { get; set; }

			public Guid PlayerGuid { get; set; }

			public Guid AgentGuid { get; set; }

			public string AgentName { get; set; } = string.Empty;

			public string AgentAuthor { get; set; } = string.Empty;

			public int GameTurn { get; set; }

			public string MemoryYaml { get; set; } = string.Empty;
		}
	}

	internal interface IAgentTurnMetricsHook
	{
		void OnTurnCompleted(AgentTurnMetrics metrics);
	}

	internal sealed class NoOpAgentTurnMetricsHook : IAgentTurnMetricsHook
	{
		public void OnTurnCompleted(AgentTurnMetrics metrics)
		{
			ArgumentNullException.ThrowIfNull(metrics);
		}
	}

	internal sealed class AgentTurnMetrics(
		Guid playerGuid,
		Guid agentGuid,
		string agentName,
		int gameTurn,
		int commandsIssued,
		int failedCommands,
		TimeSpan duration,
		bool usedHostEndTurnFallback,
		bool controllerThrew)
	{
		public Guid PlayerGuid { get; } = playerGuid;

		public Guid AgentGuid { get; } = agentGuid;

		public string AgentName { get; } = agentName ?? string.Empty;

		public int GameTurn { get; } = gameTurn;

		public int CommandsIssued { get; } = commandsIssued;

		public int FailedCommands { get; } = failedCommands;

		public TimeSpan Duration { get; } = duration;

		public bool UsedHostEndTurnFallback { get; } = usedHostEndTurnFallback;

		public bool ControllerThrew { get; } = controllerThrew;
	}

	internal interface IAgentBindingResolver
	{
		/// <summary>
		/// Resolves the effective <see cref="IAgentRegistration"/> for one runtime <see cref="Player"/>.
		/// </summary>
		/// <param name="player">The current player.</param>
		/// <returns>The resolved registration instance.</returns>
		IAgentRegistration Resolve(Player player);
	}

	/// <summary>
	/// Default binding resolver used by <see cref="TurnBasedAgentHost"/>.
	/// It first checks <see cref="AgentRegistry"/>, then falls back to built-in registration.
	/// </summary>
	internal sealed class AgentBindingResolver : IAgentBindingResolver
	{
		private readonly Dictionary<Guid, IAgentRegistration> _registrations = [];

		public IAgentRegistration Resolve(Player player)
		{
			if (AgentRegistry.Instance.TryResolve(player.PlayerGuid, out IAgentRegistration? registered))
			{
				return registered!;
			}

			if (!_registrations.TryGetValue(player.PlayerGuid, out IAgentRegistration? registration))
			{
				registration = new LegacyAgentRegistration(player);
				_registrations[player.PlayerGuid] = registration;
			}

			return registration!;
		}
	}

	/// <summary>
	/// Built-in <see cref="IAgentRegistration"/> used when no external binding exists.
	/// </summary>
	internal sealed class LegacyAgentRegistration(Player player) : IAgentRegistration
	{
		private readonly IAgentInformation _information = new LegacyAgentInformation(player);
		private readonly IAgentMemory _memory = new LegacyAgentMemory();
		private readonly ITurnBasedController _controller = new DefaultTurnBasedController();

		public IAgentInformation GetInformation() => _information;

		public IAgentMemory GetMemory() => _memory;

		public ITurnBasedController GetTurnBasedController() => _controller;
	}

	/// <summary>
	/// Built-in metadata provider for <see cref="LegacyAgentRegistration"/>.
	/// </summary>
	internal sealed class LegacyAgentInformation(Player player) : IAgentInformation
	{
		public string GetName() => "BuiltInLegacyAgent";

		public string GetAuthor() => "CivOne";

		public (int Major, int Minor, int Patch) GetVersion() => (2, 0, 0);

		public string GetDescription() => $"Built-in turn-based bridge for {player.TribeNamePlural}.";

		public Guid GetUuid() => player.PlayerGuid;
	}

	/// <summary>
	/// Built-in memory provider for <see cref="LegacyAgentRegistration"/>.
	/// </summary>
	internal sealed class LegacyAgentMemory : IAgentMemory
	{
		private string _yaml = string.Empty;

		public void SetMemory(string yaml)
		{
			_yaml = yaml ?? string.Empty;
		}

		public string GetMemory() => _yaml;
	}

	/// <summary>
	/// Built-in command-based agent implementation.
	/// It uses <see cref="ITurnContext"/> reads plus <see cref="ITurnCommandGateway"/> writes,
	/// without calling legacy <see cref="AI"/> methods directly.
	/// </summary>
	internal sealed class DefaultTurnBasedController : ITurnBasedController
	{
		private const int UnitMoveLoopLimit = 1000;
		private static readonly (int Dx, int Dy)[] _moveDeltas =
		[
			(0, -1),
			(1, 0),
			(0, 1),
			(-1, 0),
			(1, -1),
			(1, 1),
			(-1, 1),
			(-1, -1)
		];

		/// <summary>
		/// Executes one turn by reading <see cref="ITurnContext"/>, issuing commands via
		/// <see cref="ITurnCommandGateway"/>, and finalizing through <see cref="ITurnSession.EndTurn"/>.
		/// </summary>
		/// <param name="session">The host-provided turn session.</param>
		public void OnTurn(ITurnSession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			if (session.Context.CurrentResearchName is null && session.Context.AvailableResearchNames.Count > 0)
			{
				_ = session.Commands.Research.ChooseResearch(session.Context.AvailableResearchNames[0]);
			}

			foreach (ICityView city in session.Context.OwnCities)
			{
				if (city.CurrentProductionName is not null || city.AvailableProductionNames.Count == 0)
				{
					continue;
				}

				_ = session.Commands.Cities.ChooseProduction(city.Id, city.AvailableProductionNames[0]);
			}

			for (int i = 0; i < UnitMoveLoopLimit; i++)
			{
				IReadOnlyList<IUnitView> movableUnits =
					[.. session.Context.OwnUnits.Where(unit => unit.HasMovesLeft && !unit.HasAction)];

				if (movableUnits.Count == 0)
				{
					break;
				}

				bool anyMoveApplied = false;
				foreach (IUnitView unit in movableUnits)
				{
					if (TryMoveUnit(session, unit.Id))
					{
						anyMoveApplied = true;
					}
				}

				if (!anyMoveApplied)
				{
					break;
				}
			}

			session.EndTurn();
		}

		private static bool TryMoveUnit(ITurnSession session, Guid unitId)
		{
			foreach ((int dx, int dy) in _moveDeltas)
			{
				ICommandResult result = session.Commands.Units.Move(unitId, dx, dy);
				if (result.Success)
				{
					return true;
				}

				if (result.ErrorCode == "TURN_ALREADY_ENDED" || result.ErrorCode == "TOTAL_ACTION_LIMIT_REACHED" || result.ErrorCode == "ACTION_TYPE_LIMIT_REACHED")
				{
					return false;
				}
			}

			return false;
		}
	}

	internal sealed class TurnSession : ITurnSession
	{
		private const int TotalCommandLimit = 100;
		private static readonly Dictionary<string, int> PerTypeCommandLimits = new(StringComparer.OrdinalIgnoreCase)
		{
			["Move"] = 1000,
			["Fortify"] = 100,
			["Wake"] = 100,
			["SetGoto"] = 100,
			["ClearGoto"] = 100,
			["Disband"] = 50,
			["FoundCity"] = 20,
			["BuildRoad"] = 100,
			["BuildIrrigation"] = 100,
			["BuildMine"] = 100,
			["ChooseProduction"] = 200,
			["ChooseResearch"] = 20
		};

		private readonly Dictionary<string, int> _commandCounts = new(StringComparer.OrdinalIgnoreCase);
		private readonly RingEventJournal _journal;
		private int _totalCommandCount;
		private int _failedCommandCount;

		/// <summary>
		/// Creates one runtime turn session that wires
		/// <see cref="ITurnContext"/>, <see cref="IEventJournal"/>, and <see cref="ITurnCommandGateway"/>.
		/// </summary>
		/// <param name="player">The current runtime player.</param>
		/// <param name="journal">The per-player journal implementation.</param>
		public TurnSession(Player player, RingEventJournal journal)
		{
			_journal = journal;
			Context = new TurnContext(player);
			Events = journal;
			Commands = new TurnCommandGateway(this, player, journal);
		}

		public ITurnContext Context { get; }

		public IEventJournal Events { get; }

		public ITurnCommandGateway Commands { get; }

		internal int TotalCommandCount => _totalCommandCount;

		internal int FailedCommandCount => _failedCommandCount;

		internal bool IsTurnEnded { get; private set; }

		/// <summary>
		/// Marks the session as ended.
		/// Host and controller both can call this.
		/// </summary>
		public void EndTurn()
		{
			IsTurnEnded = true;
		}

		/// <summary>
		/// Validates command limits before actual command execution.
		/// </summary>
		/// <param name="commandKind">Logical command kind key.</param>
		/// <param name="failureResult">Failure result when validation rejects command.</param>
		/// <returns><see langword="true"/> when command may execute; otherwise <see langword="false"/>.</returns>
		internal bool TryBeginCommand(string commandKind, out CommandResult failureResult)
		{
			ulong sequenceBefore = _journal.CurrentSequence;
			if (IsTurnEnded)
			{
				_failedCommandCount++;
				failureResult = new CommandResult(false, "TURN_ALREADY_ENDED", "Turn already ended.", sequenceBefore, _journal.CurrentSequence);
				return false;
			}

			int nextTotalCommandCount = _totalCommandCount + 1;
			if (nextTotalCommandCount > TotalCommandLimit)
			{
				_failedCommandCount++;
				EndTurn();
				failureResult = new CommandResult(false, "TOTAL_ACTION_LIMIT_REACHED", "Total action limit reached.", sequenceBefore, _journal.CurrentSequence);
				return false;
			}

			int previousCount = _commandCounts.TryGetValue(commandKind, out int count) ? count : 0;
			int nextCount = previousCount + 1;
			if (PerTypeCommandLimits.TryGetValue(commandKind, out int perTypeLimit) && nextCount > perTypeLimit)
			{
				_failedCommandCount++;
				EndTurn();
				failureResult = new CommandResult(false, "ACTION_TYPE_LIMIT_REACHED", $"Action type limit reached for {commandKind}.", sequenceBefore, _journal.CurrentSequence);
				return false;
			}

			_commandCounts[commandKind] = nextCount;
			_totalCommandCount = nextTotalCommandCount;
			failureResult = default;
			return true;
		}

		/// <summary>
		/// Creates a command result using the current journal sequence boundaries.
		/// </summary>
		internal CommandResult BuildResult(bool success, string errorCode, string? errorMessage, ulong sequenceBefore)
		{
			if (!success)
			{
				_failedCommandCount++;
			}

			return new CommandResult(success, errorCode, errorMessage, sequenceBefore, _journal.CurrentSequence);
		}
	}

	/// <summary>
	/// Runtime implementation of <see cref="ITurnContext"/>.
	/// </summary>
	internal sealed class TurnContext(Player player) : ITurnContext
	{
		private readonly byte _ownerId = Game.Instance.PlayerNumber(player);
		private readonly IPlayer _playerState = player;

		public int GameTurn => Game.Instance.GameTurn;

		public ICivilizationView CurrentCivilization => new CivilizationView(player);

		public IMapView Map => new MapView(player, _playerState);

		public IReadOnlyList<IUnitView> OwnUnits =>
			[.. Game.Instance.GetUnits().Where(unit => unit.Owner == _ownerId).Select(unit => new UnitView(unit))];

		public IReadOnlyList<ICityView> OwnCities =>
			[.. Game.Instance.GetCities().Where(city => city.Owner == _ownerId && city.Size > 0).Select(city => new CityView(city))];

		public IReadOnlyList<string> AvailableResearchNames =>
			[.. player.AvailableResearch.Select(advance => advance.GetType().Name)];

		public string? CurrentResearchName => player.CurrentResearch?.GetType().Name;
	}

	/// <summary>
	/// Runtime implementation of <see cref="ITurnCommandGateway"/>.
	/// </summary>
	internal sealed class TurnCommandGateway : ITurnCommandGateway
	{
		public TurnCommandGateway(TurnSession session, Player player, RingEventJournal journal)
		{
			Units = new UnitCommandGateway(session, player, journal);
			Cities = new CityCommandGateway(session, player, journal);
			Research = new ResearchCommandGateway(session, player, journal);
		}

		public IUnitCommandGateway Units { get; }

		public ICityCommandGateway Cities { get; }

		public IResearchCommandGateway Research { get; }
	}

	/// <summary>
	/// Runtime unit command gateway implementation.
	/// </summary>
	internal sealed class UnitCommandGateway(TurnSession session, Player player, RingEventJournal journal) : IUnitCommandGateway
	{
		private readonly byte _ownerId = Game.Instance.PlayerNumber(player);

		public ICommandResult Move(Guid unitId, int dx, int dy)
		{
			if (!session.TryBeginCommand("Move", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			if (!unit.MoveTo(dx, dy))
			{
				return session.BuildResult(false, "MOVE_REJECTED", "Move was rejected by runtime validation.", sequenceBefore);
			}

			journal.Append(AgentEventKind.UnitMoved, unit.Id, unit.GetType().Name, "Unit moved.");
			journal.Append(AgentEventKind.TilesExplored, unit.Id, null, "Visibility may have changed.");
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult Fortify(Guid unitId)
		{
			if (!session.TryBeginCommand("Fortify", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			unit.Fortify = true;
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult Wake(Guid unitId)
		{
			if (!session.TryBeginCommand("Wake", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			unit.Sentry = false;
			unit.Fortify = false;
			unit.Busy = false;
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult SetGoto(Guid unitId, int x, int y)
		{
			if (!session.TryBeginCommand("SetGoto", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			unit.Goto = new Point(x, y);
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult ClearGoto(Guid unitId)
		{
			if (!session.TryBeginCommand("ClearGoto", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			unit.Goto = Point.Empty;
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult Disband(Guid unitId)
		{
			if (!session.TryBeginCommand("Disband", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is null)
			{
				return session.BuildResult(false, "UNIT_NOT_FOUND", "Unit not found or not owned by current player.", sequenceBefore);
			}

			TurnBasedAgentHost.MarkUnitAsExplicitDisband(unit.Id);
			try
			{
				Game.Instance.DisbandUnit(unit);
			}
			finally
			{
				TurnBasedAgentHost.UnmarkUnitAsExplicitDisband(unit.Id);
			}

			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult FoundCity(Guid unitId)
		{
			if (!session.TryBeginCommand("FoundCity", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is not Settlers settlers)
			{
				return session.BuildResult(false, "INVALID_UNIT_TYPE", "FoundCity requires Settlers.", sequenceBefore);
			}

			GameTask.Enqueue(Orders.FoundCity(settlers));
			journal.Append(AgentEventKind.CityProductionChanged, settlers.Id, nameof(Settlers), "Found city order queued.");
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult BuildRoad(Guid unitId)
		{
			if (!session.TryBeginCommand("BuildRoad", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is not Settlers settlers)
			{
				return session.BuildResult(false, "INVALID_UNIT_TYPE", "BuildRoad requires Settlers.", sequenceBefore);
			}

			GameTask.Enqueue(Orders.BuildRoad(settlers));
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult BuildIrrigation(Guid unitId)
		{
			if (!session.TryBeginCommand("BuildIrrigation", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is not Settlers settlers)
			{
				return session.BuildResult(false, "INVALID_UNIT_TYPE", "BuildIrrigation requires Settlers.", sequenceBefore);
			}

			GameTask.Enqueue(Orders.BuildIrrigation(settlers));
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		public ICommandResult BuildMine(Guid unitId)
		{
			if (!session.TryBeginCommand("BuildMine", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			IUnit? unit = GetOwnUnit(unitId);
			if (unit is not Settlers settlers)
			{
				return session.BuildResult(false, "INVALID_UNIT_TYPE", "BuildMine requires Settlers.", sequenceBefore);
			}

			GameTask.Enqueue(Orders.BuildMines(settlers));
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}

		private IUnit? GetOwnUnit(Guid unitId)
		{
			return Game.Instance.GetUnits().FirstOrDefault(unit => unit.Owner == _ownerId && unit.Id == unitId);
		}
	}

	/// <summary>
	/// Runtime city command gateway implementation.
	/// </summary>
	internal sealed class CityCommandGateway(TurnSession session, Player player, RingEventJournal journal) : ICityCommandGateway
	{
		private readonly byte _ownerId = Game.Instance.PlayerNumber(player);

		public ICommandResult ChooseProduction(Guid cityId, string productionName)
		{
			if (!session.TryBeginCommand("ChooseProduction", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			if (string.IsNullOrWhiteSpace(productionName))
			{
				return session.BuildResult(false, "INVALID_PRODUCTION_NAME", "Production name must not be empty.", sequenceBefore);
			}

			City? city = Game.Instance.GetCities().FirstOrDefault(c => c.Owner == _ownerId && c.Id == cityId && c.Size > 0);
			if (city is null)
			{
				return session.BuildResult(false, "CITY_NOT_FOUND", "City not found or not owned by current player.", sequenceBefore);
			}

			IProduction? production = city.AvailableProduction.FirstOrDefault(p => string.Equals(p.GetType().Name, productionName, StringComparison.OrdinalIgnoreCase));
			if (production is null)
			{
				return session.BuildResult(false, "PRODUCTION_NOT_AVAILABLE", $"Production '{productionName}' is not available.", sequenceBefore);
			}

			city.SetProduction(production);
			journal.Append(AgentEventKind.CityProductionChanged, city.Id, production.GetType().Name, "City production changed.");
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}
	}

	/// <summary>
	/// Runtime research command gateway implementation.
	/// </summary>
	internal sealed class ResearchCommandGateway(TurnSession session, Player player, RingEventJournal journal) : IResearchCommandGateway
	{
		public ICommandResult ChooseResearch(string researchName)
		{
			if (!session.TryBeginCommand("ChooseResearch", out CommandResult failed))
			{
				return failed;
			}

			ulong sequenceBefore = journal.CurrentSequence;
			if (string.IsNullOrWhiteSpace(researchName))
			{
				return session.BuildResult(false, "INVALID_RESEARCH_NAME", "Research name must not be empty.", sequenceBefore);
			}

			IAdvance? advance = player.AvailableResearch.FirstOrDefault(a => string.Equals(a.GetType().Name, researchName, StringComparison.OrdinalIgnoreCase));
			if (advance is null)
			{
				return session.BuildResult(false, "RESEARCH_NOT_AVAILABLE", $"Research '{researchName}' is not available.", sequenceBefore);
			}

			player.CurrentResearch = advance;
			journal.Append(AgentEventKind.ResearchChanged, player.PlayerGuid, advance.GetType().Name, "Current research changed.");
			return session.BuildResult(true, string.Empty, null, sequenceBefore);
		}
	}

	/// <summary>
	/// Bounded per-player journal implementation for <see cref="IEventJournal"/>.
	/// </summary>
	internal sealed class RingEventJournal : IEventJournal
	{
		private const int MaxEvents = 100;
		private readonly List<AgentEvent> _events = [];
		private ulong _nextSequence = 1;

		public ulong CurrentSequence => _nextSequence - 1;

		public EventReadResult ReadSince(ulong sequence)
		{
			if (_events.Count == 0)
			{
				return new EventReadResult(false, false, sequence, CurrentSequence, []);
			}

			ulong oldest = _events[0].Sequence;
			if (sequence + 1 < oldest)
			{
				return new EventReadResult(true, true, oldest, CurrentSequence, [.. _events]);
			}

			AgentEvent[] resultEvents = [.. _events.Where(e => e.Sequence > sequence).OrderBy(e => e.Sequence)];
			ulong from = resultEvents.Length > 0 ? resultEvents[0].Sequence : sequence;
			ulong to = resultEvents.Length > 0 ? resultEvents[^1].Sequence : CurrentSequence;
			return new EventReadResult(false, false, from, to, resultEvents);
		}

		internal void Append(AgentEventKind kind, Guid? entityId, string? name, string? message)
		{
			_events.Add(new AgentEvent(_nextSequence++, kind, entityId, name, message));
			if (_events.Count > MaxEvents)
			{
				_events.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Runtime adapter for <see cref="ICivilizationView"/>.
	/// </summary>
	internal sealed class CivilizationView(Player player) : ICivilizationView
	{
		public Guid Id => player.PlayerGuid;

		public string LeaderName => player.LeaderName;

		public string CivilizationName => player.Civilization.Name;

		public string CivilizationNamePlural => player.TribeNamePlural;

		public short Gold => player.Gold;

		public int LuxuriesRate => player.LuxuriesRate;

		public int TaxesRate => player.TaxesRate;

		public int ScienceRate => player.ScienceRate;
	}

	/// <summary>
	/// Runtime adapter for <see cref="IUnitView"/>.
	/// </summary>
	internal sealed class UnitView(IUnit unit) : IUnitView
	{
		public Guid Id => unit.Id;

		public Guid OwnerId => Game.Instance.GetPlayer(unit.Owner).PlayerGuid;

		public string Name => unit.GetType().Name;

		public int X => unit.X;

		public int Y => unit.Y;

		public Point Goto => unit.Goto;

		public byte MovesLeft => unit.MovesLeft;

		public byte PartMoves => unit.PartMoves;

		public bool HasAction => unit.HasAction;

		public bool HasMovesLeft => unit.HasMovesLeft;

		public bool Sentry => unit.Sentry;

		public bool FortifyActive => unit.FortifyActive;

		public bool Fortify => unit.Fortify;

		public bool Veteran => unit.Veteran;
	}

	/// <summary>
	/// Runtime adapter for <see cref="ICityView"/>.
	/// </summary>
	internal sealed class CityView(City city) : ICityView
	{
		public Guid Id => city.Id;

		public Guid OwnerId => city.Player.PlayerGuid;

		public string Name => city.Name;

		public int X => city.X;

		public int Y => city.Y;

		public byte Size => city.Size;

		public int Food => city.Food;

		public int Shields => city.Shields;

		public string? CurrentProductionName => city.CurrentProduction?.GetType().Name;

		public IReadOnlyList<string> AvailableProductionNames => [.. city.AvailableProduction.Select(p => p.GetType().Name)];

		public IReadOnlyList<string> BuildingNames => [.. city.Buildings.Select(b => b.GetType().Name)];

		public IReadOnlyList<string> WonderNames => [.. city.Wonders.Select(w => w.GetType().Name)];

		public IReadOnlyList<uint> VisibleSizes => [.. city.VisibleSizes];
	}

	/// <summary>
	/// Runtime adapter for <see cref="ITileView"/>.
	/// </summary>
	internal sealed class TileView(ITile tile, bool visible, bool explored) : ITileView
	{
		public int X => tile.X;

		public int Y => tile.Y;

		public bool IsVisible => visible;

		public bool IsExplored => explored;

		public string TerrainName => tile.Type.ToString();

		public bool Road => tile.Road;

		public bool Railroad => tile.RailRoad;

		public bool Irrigation => tile.Irrigation;

		public bool Mine => tile.Mine;

		public bool Pollution => tile.Pollution;

		public int ContinentId => tile.ContinentId;

		public Guid? CityId => visible && tile.City is not null ? tile.City.Id : null;

		public IReadOnlyList<Guid> UnitIds => !visible ? [] : [.. tile.Units.Select(unit => unit.Id)];
	}

	/// <summary>
	/// Runtime adapter for <see cref="IMapView"/> and tile lookup.
	/// </summary>
	internal sealed class MapView(Player player, IPlayer playerState) : IMapView
	{
		public int Width => Map.WIDTH;

		public int Height => Map.HEIGHT;

		public ITileView? GetTile(int x, int y)
		{
			if (y < 0 || y >= Map.HEIGHT)
			{
				return null;
			}

			while (x < 0)
			{
				x += Map.WIDTH;
			}
			while (x >= Map.WIDTH)
			{
				x -= Map.WIDTH;
			}

			bool explored = playerState.Explored[x, y];
			if (!explored)
			{
				return null;
			}

			bool visible = player.Visible(x, y);
			ITile tile = Map.Instance[x, y];
			return new TileView(tile, visible, explored);
		}
	}
}
