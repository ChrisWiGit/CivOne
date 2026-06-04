using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Agents;
using Xunit;

namespace CivOne.UnitTests
{
	public class AgentControllerResilienceTests
	{
		[Theory]
		[InlineData("TURN_ALREADY_ENDED")]
		[InlineData("TOTAL_ACTION_LIMIT_REACHED")]
		[InlineData("ACTION_TYPE_LIMIT_REACHED")]
		[InlineData("UNIT_NOT_FOUND")]
		[InlineData("MOVE_REJECTED")]
		[InlineData("INVALID_UNIT_TYPE")]
		[InlineData("INVALID_PRODUCTION_NAME")]
		[InlineData("CITY_NOT_FOUND")]
		[InlineData("PRODUCTION_NOT_AVAILABLE")]
		[InlineData("INVALID_RESEARCH_NAME")]
		[InlineData("RESEARCH_NOT_AVAILABLE")]
		public void OnTurn_WhenCommandReturnsKnownErrorCode_HandlesWithoutThrowing(string errorCode)
		{
			// Arrange
			ResilientController testee = new();
			FakeContext context = CreateContext();
			FakeCommandGateway commands = new(
				new FakeResearchGateway(errorCode),
				new FakeCityGateway(errorCode),
				new FakeUnitGateway(errorCode));
			FakeSession session = new(context, commands, new FakeEventJournal(sequence => new(false, false, sequence, sequence, [])));

			// Act
			Exception? actual = Record.Exception(() => testee.OnTurn(session));

			// Assert
			Assert.Null(actual);
			Assert.True(session.EndTurnCalled);
			Assert.Contains(errorCode, testee.HandledErrorCodes);
		}

		[Fact]
		public void OnTurn_WhenCursorExpired_TriggersFullResyncPathAndAdvancesCursor()
		{
			// Arrange
			ResilientController testee = new();
			FakeContext context = CreateContext();
			FakeCommandGateway commands = new(
				new FakeResearchGateway(string.Empty, success: true),
				new FakeCityGateway(string.Empty, success: true),
				new FakeUnitGateway(string.Empty, success: true));
			FakeEventJournal journal = new(_ => new(true, true, 11, 77, []));
			FakeSession session = new(context, commands, journal);

			// Act
			testee.OnTurn(session);

			// Assert
			Assert.True(session.EndTurnCalled);
			Assert.Equal(1, testee.ResyncCount);
			Assert.Equal(77UL, testee.LastSequence);
		}

		private static FakeContext CreateContext()
		{
			return new FakeContext
			{
				CurrentResearchName = null,
				AvailableResearchNames = ["Alphabet"],
				OwnCities =
				[
					new FakeCityView
					{
						Id = Guid.NewGuid(),
						CurrentProductionName = null,
						AvailableProductionNames = ["Barracks"]
					}
				],
				OwnUnits =
				[
					new FakeUnitView
					{
						Id = Guid.NewGuid(),
						HasMovesLeft = true,
						HasAction = false
					}
				]
			};
		}

		private sealed class ResilientController : ITurnBasedController
		{
			private readonly List<string> _handledErrorCodes = [];

			public IReadOnlyList<string> HandledErrorCodes => _handledErrorCodes;
			public int ResyncCount { get; private set; }
			public ulong LastSequence { get; private set; }

			public void OnTurn(ITurnSession session)
			{
				EventReadResult delta = session.Events.ReadSince(LastSequence);
				if (delta.RequiresFullResync)
				{
					RebuildFromContext(session.Context);
				}
				LastSequence = delta.ToSequence;

				if (session.Context.AvailableResearchNames.Count > 0)
				{
					ICommandResult research = session.Commands.Research.ChooseResearch(session.Context.AvailableResearchNames[0]);
					HandleCommandResult(research);
				}

				ICityView? city = session.Context.OwnCities.FirstOrDefault();
				if (city is not null && city.AvailableProductionNames.Count > 0)
				{
					ICommandResult production = session.Commands.Cities.ChooseProduction(city.Id, city.AvailableProductionNames[0]);
					HandleCommandResult(production);
				}

				IUnitView? unit = session.Context.OwnUnits.FirstOrDefault();
				if (unit is not null)
				{
					ICommandResult move = session.Commands.Units.Move(unit.Id, 0, 1);
					HandleCommandResult(move);
				}

				session.EndTurn();
			}

			private void RebuildFromContext(ITurnContext context)
			{
				ResyncCount++;
				_ = context.CurrentCivilization;
				_ = context.OwnCities.Count;
				_ = context.OwnUnits.Count;
			}

			private void HandleCommandResult(ICommandResult result)
			{
				if (result.Success)
				{
					return;
				}

				switch (result.ErrorCode)
				{
					case "TURN_ALREADY_ENDED":
					case "TOTAL_ACTION_LIMIT_REACHED":
					case "ACTION_TYPE_LIMIT_REACHED":
					case "UNIT_NOT_FOUND":
					case "MOVE_REJECTED":
					case "INVALID_UNIT_TYPE":
					case "INVALID_PRODUCTION_NAME":
					case "CITY_NOT_FOUND":
					case "PRODUCTION_NOT_AVAILABLE":
					case "INVALID_RESEARCH_NAME":
					case "RESEARCH_NOT_AVAILABLE":
						_handledErrorCodes.Add(result.ErrorCode);
						break;
					default:
						_handledErrorCodes.Add($"UNKNOWN:{result.ErrorCode}");
						break;
				}
			}
		}

		private sealed class FakeSession(FakeContext context, ITurnCommandGateway commands, IEventJournal events) : ITurnSession
		{
			public bool EndTurnCalled { get; private set; }
			public ITurnContext Context { get; } = context;
			public IEventJournal Events { get; } = events;
			public ITurnCommandGateway Commands { get; } = commands;

			public void EndTurn()
			{
				EndTurnCalled = true;
			}
		}

		private sealed class FakeContext : ITurnContext
		{
			public int GameTurn { get; init; }
			public ICivilizationView CurrentCivilization { get; init; } = new FakeCivilizationView();
			public IMapView Map { get; init; } = new FakeMapView();
			public IReadOnlyList<IUnitView> OwnUnits { get; init; } = [];
			public IReadOnlyList<ICityView> OwnCities { get; init; } = [];
			public IReadOnlyList<string> AvailableResearchNames { get; init; } = [];
			public string? CurrentResearchName { get; init; }
		}

		private sealed class FakeCommandGateway(
			IResearchCommandGateway research,
			ICityCommandGateway cities,
			IUnitCommandGateway units) : ITurnCommandGateway
		{
			public IUnitCommandGateway Units { get; } = units;
			public ICityCommandGateway Cities { get; } = cities;
			public IResearchCommandGateway Research { get; } = research;
		}

		private sealed class FakeResearchGateway(string errorCode, bool success = false) : IResearchCommandGateway
		{
			public ICommandResult ChooseResearch(string researchName)
			{
				return new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			}
		}

		private sealed class FakeCityGateway(string errorCode, bool success = false) : ICityCommandGateway
		{
			public ICommandResult ChooseProduction(Guid cityId, string productionName)
			{
				return new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			}
		}

		private sealed class FakeUnitGateway(string errorCode, bool success = false) : IUnitCommandGateway
		{
			public ICommandResult Move(Guid unitId, int dx, int dy) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult Fortify(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult Wake(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult SetGoto(Guid unitId, int x, int y) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult ClearGoto(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult Disband(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult FoundCity(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult BuildRoad(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult BuildIrrigation(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
			public ICommandResult BuildMine(Guid unitId) => new CommandResult(success, errorCode, success ? null : "failed", 0, 0);
		}

		private sealed class FakeEventJournal(Func<ulong, EventReadResult> readSince) : IEventJournal
		{
			public ulong CurrentSequence => 0;

			public EventReadResult ReadSince(ulong sequence)
			{
				return readSince(sequence);
			}
		}

		private sealed class FakeCivilizationView : ICivilizationView
		{
			public Guid Id { get; init; } = Guid.NewGuid();
			public string LeaderName { get; init; } = "Leader";
			public string CivilizationName { get; init; } = "Civ";
			public string CivilizationNamePlural { get; init; } = "Civs";
			public short Gold { get; init; }
			public int LuxuriesRate { get; init; }
			public int TaxesRate { get; init; }
			public int ScienceRate { get; init; }
		}

		private sealed class FakeMapView : IMapView
		{
			public int Width => 0;
			public int Height => 0;
			public ITileView? GetTile(int x, int y) => null;
		}

		private sealed class FakeCityView : ICityView
		{
			public Guid Id { get; init; }
			public Guid OwnerId { get; init; }
			public string Name { get; init; } = "City";
			public int X { get; init; }
			public int Y { get; init; }
			public byte Size { get; init; }
			public int Food { get; init; }
			public int Shields { get; init; }
			public string? CurrentProductionName { get; init; }
			public IReadOnlyList<string> AvailableProductionNames { get; init; } = [];
			public IReadOnlyList<string> BuildingNames { get; init; } = [];
			public IReadOnlyList<string> WonderNames { get; init; } = [];
			public IReadOnlyList<uint> VisibleSizes { get; init; } = [];
		}

		private sealed class FakeUnitView : IUnitView
		{
			public Guid Id { get; init; }
			public Guid OwnerId { get; init; }
			public string Name { get; init; } = "Unit";
			public int X { get; init; }
			public int Y { get; init; }
			public Point Goto { get; init; }
			public byte MovesLeft { get; init; }
			public byte PartMoves { get; init; }
			public bool HasAction { get; init; }
			public bool HasMovesLeft { get; init; }
			public bool Sentry { get; init; }
			public bool FortifyActive { get; init; }
			public bool Fortify { get; init; }
			public bool Veteran { get; init; }
		}
	}
}
