using System;
using System.Collections.Generic;
using System.Drawing;
using CivOne.Agents;
using Xunit;

namespace CivOne.UnitTests
{
	public class AgentControllerTests
	{
		[Fact]
		public void OnTurn_WhenResearchMissing_ChoosesFirstResearchName()
		{
			// Arrange
			FakeResearchGateway researchGateway = new();
			FakeCommandGateway commandGateway = new(researchGateway, new FakeCityGateway(), new FakeUnitGateway());
			FakeContext context = new()
			{
				CurrentResearchName = null,
				AvailableResearchNames = ["Alphabet", "BronzeWorking"],
				OwnCities = [],
				OwnUnits = []
			};
			FakeSession session = new(context, commandGateway);
			DefaultTurnBasedController testee = new();

			// Act
			testee.OnTurn(session);

			// Assert
			Assert.True(session.EndTurnCalled);
			Assert.Single(researchGateway.Calls);
			Assert.Equal("Alphabet", researchGateway.Calls[0]);
		}

		[Fact]
		public void OnTurn_WhenCityProductionMissing_ChoosesFirstProductionName()
		{
			// Arrange
			FakeCityGateway cityGateway = new();
			FakeCommandGateway commandGateway = new(new FakeResearchGateway(), cityGateway, new FakeUnitGateway());
			Guid cityId = Guid.NewGuid();
			FakeContext context = new()
			{
				CurrentResearchName = "Alphabet",
				AvailableResearchNames = [],
				OwnCities =
				[
					new FakeCityView
					{
						Id = cityId,
						CurrentProductionName = null,
						AvailableProductionNames = ["Barracks", "Militia"]
					}
				],
				OwnUnits = []
			};
			FakeSession session = new(context, commandGateway);
			DefaultTurnBasedController testee = new();

			// Act
			testee.OnTurn(session);

			// Assert
			Assert.True(session.EndTurnCalled);
			Assert.Single(cityGateway.Calls);
			Assert.Equal(cityId, cityGateway.Calls[0].CityId);
			Assert.Equal("Barracks", cityGateway.Calls[0].ProductionName);
		}

		[Fact]
		public void OnTurn_WhenActionTypeLimitReached_StopsMoveAttemptsEarly()
		{
			// Arrange
			FakeUnitGateway unitGateway = new
			(
				new CommandResult(false, "ACTION_TYPE_LIMIT_REACHED", "limit", 0, 0)
			);
			FakeCommandGateway commandGateway = new(new FakeResearchGateway(), new FakeCityGateway(), unitGateway);
			FakeContext context = new()
			{
				CurrentResearchName = "Alphabet",
				AvailableResearchNames = [],
				OwnCities = [],
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
			FakeSession session = new(context, commandGateway);
			DefaultTurnBasedController testee = new();

			// Act
			testee.OnTurn(session);

			// Assert
			Assert.True(session.EndTurnCalled);
			Assert.Equal(1, unitGateway.MoveCalls);
		}

		[Fact]
		public void OnTurn_WhenResearchCityAndMoveCommandsFail_DoesNotThrowAndEndsTurn()
		{
			// Arrange
			FakeResearchGateway researchGateway = new(new CommandResult(false, "INVALID_RESEARCH_NAME", "invalid", 0, 0));
			FakeCityGateway cityGateway = new(new CommandResult(false, "PRODUCTION_NOT_AVAILABLE", "missing", 0, 0));
			FakeUnitGateway unitGateway = new(new CommandResult(false, "MOVE_REJECTED", "blocked", 0, 0));
			FakeCommandGateway commandGateway = new(researchGateway, cityGateway, unitGateway);

			FakeContext context = new()
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

			FakeSession session = new(context, commandGateway);
			DefaultTurnBasedController testee = new();

			// Act
			Exception? actual = Record.Exception(() => testee.OnTurn(session));

			// Assert
			Assert.Null(actual);
			Assert.True(session.EndTurnCalled);
			Assert.Single(researchGateway.Calls);
			Assert.Single(cityGateway.Calls);
			Assert.True(unitGateway.MoveCalls >= 1);
		}

		[Theory]
		[InlineData("TURN_ALREADY_ENDED")]
		[InlineData("TOTAL_ACTION_LIMIT_REACHED")]
		[InlineData("ACTION_TYPE_LIMIT_REACHED")]
		public void OnTurn_WhenMoveReturnsStopErrorCode_StopsMoveAttemptsAfterFirstTry(string errorCode)
		{
			// Arrange
			FakeUnitGateway unitGateway = new(new CommandResult(false, errorCode, "stop", 0, 0));
			FakeCommandGateway commandGateway = new(new FakeResearchGateway(), new FakeCityGateway(), unitGateway);
			FakeContext context = new()
			{
				CurrentResearchName = "Alphabet",
				AvailableResearchNames = [],
				OwnCities = [],
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

			FakeSession session = new(context, commandGateway);
			DefaultTurnBasedController testee = new();

			// Act
			testee.OnTurn(session);

			// Assert
			Assert.True(session.EndTurnCalled);
			Assert.Equal(1, unitGateway.MoveCalls);
		}

		private sealed class FakeSession(FakeContext context, ITurnCommandGateway commands) : ITurnSession
		{
			public bool EndTurnCalled { get; private set; }

			public ITurnContext Context { get; } = context;

			public IEventJournal Events { get; } = new FakeEventJournal();

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

		private sealed class FakeResearchGateway(ICommandResult? result = null) : IResearchCommandGateway
		{
			public List<string> Calls { get; } = [];

			public ICommandResult ChooseResearch(string researchName)
			{
				Calls.Add(researchName);
				return result ?? new CommandResult(true, string.Empty, null, 0, 0);
			}
		}

		private sealed class FakeCityGateway(ICommandResult? result = null) : ICityCommandGateway
		{
			public List<(Guid CityId, string ProductionName)> Calls { get; } = [];

			public ICommandResult ChooseProduction(Guid cityId, string productionName)
			{
				Calls.Add((cityId, productionName));
				return result ?? new CommandResult(true, string.Empty, null, 0, 0);
			}
		}

		private sealed class FakeUnitGateway(ICommandResult? moveResult = null) : IUnitCommandGateway
		{
			public int MoveCalls { get; private set; }

			public ICommandResult Move(Guid unitId, int dx, int dy)
			{
				MoveCalls++;
				return moveResult ?? new CommandResult(false, "MOVE_REJECTED", null, 0, 0);
			}

			public ICommandResult Fortify(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult Wake(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult SetGoto(Guid unitId, int x, int y) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult ClearGoto(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult Disband(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult FoundCity(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult BuildRoad(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult BuildIrrigation(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
			public ICommandResult BuildMine(Guid unitId) => new CommandResult(true, string.Empty, null, 0, 0);
		}

		private sealed class FakeEventJournal : IEventJournal
		{
			public ulong CurrentSequence => 0;
			public EventReadResult ReadSince(ulong sequence) => new(false, false, sequence, sequence, []);
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
