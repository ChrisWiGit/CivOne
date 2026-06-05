using System;
using System.Collections.Generic;
using System.Drawing;
using CivOne.Services.Pathfinding;
using CivOne.Services.Random;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	public class AiGotoExecutorTests
	{
		[Fact]
		public void CreateFor_WhenComputerPathfindingEnabled_ReturnsSmartExecutor()
		{
			// Arrange
			IAiGotoExecutor expected = new StubExecutor(AiGotoExecutionResult.NotHandled);
			IAiGotoExecutor fallback = new StubExecutor(AiGotoExecutionResult.NotHandled);
			IRandomService randomService = new StubRandomService();
			var testee = new AiGotoExecutorFactory(() => true, expected, fallback, randomService: randomService);

			// Act
			IAiGotoExecutor actual = testee.CreateFor(new MockedIUnit());

			// Assert
			Assert.Same(expected, actual);
		}

		[Fact]
		public void CreateFor_WhenComputerPathfindingDisabled_ReturnsNoOpExecutor()
		{
			// Arrange
			IAiGotoExecutor smart = new StubExecutor(AiGotoExecutionResult.NotHandled);
			IAiGotoExecutor expected = new StubExecutor(AiGotoExecutionResult.NotHandled);
			IRandomService randomService = new StubRandomService();
			var testee = new AiGotoExecutorFactory(() => false, smart, expected, randomService: randomService);

			// Act
			IAiGotoExecutor actual = testee.CreateFor(new MockedIUnit());

			// Assert
			Assert.Same(expected, actual);
		}

		[Fact]
		public void TryExecute_WhenUnitIsNull_ReturnsNotHandled()
		{
			// Arrange
			var testee = CreateSmartExecutor(PathStepResult.Disabled());

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(null);

			// Assert
			Assert.Equal(AiGotoExecutionResult.NotHandled, actual);
		}

		[Fact]
		public void TryExecute_WhenUnitGotoIsEmpty_ReturnsNotHandled()
		{
			// Arrange
			TestUnit unit = new() { Goto = Point.Empty };
			var testee = CreateSmartExecutor(PathStepResult.Disabled());

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.NotHandled, actual);
		}

		[Fact]
		public void TryExecute_WhenPathfinderDisabled_ReturnsNotHandled()
		{
			// Arrange
			TestUnit unit = new() { Goto = new Point(12, 10) };
			var testee = CreateSmartExecutor(PathStepResult.Disabled());

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.NotHandled, actual);
		}

		[Fact]
		public void TryExecute_WhenNoPath_ClearsGotoAndReturnsContinue()
		{
			// Arrange
			TestUnit unit = new() { Goto = new Point(12, 10) };
			var testee = CreateSmartExecutor(PathStepResult.NoPath());

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.Continue, actual);
			Assert.Equal(Point.Empty, unit.Goto);
		}

		[Fact]
		public void TryExecute_WhenPathStepTileNotReachable_ClearsGotoAndReturnsContinue()
		{
			// Arrange
			TestUnit unit = new() { Goto = new Point(12, 10), MoveTargets = [] };
			var testee = CreateSmartExecutor(PathStepResult.Success(12, 10));

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.Continue, actual);
			Assert.Equal(Point.Empty, unit.Goto);
		}

		[Fact]
		public void TryExecute_WhenCivilianWouldAttack_ClearsGotoAndReturnsContinue()
		{
			// Arrange
			var enemy = new MockedIUnit { Owner = 2 };
			ITile targetTile = new MockedGrassland(11, 10).WithUnits(enemy);
			TestUnit unit = new()
			{
				Owner = 1,
				Role = CivOne.Enums.UnitRole.Civilian,
				Goto = new Point(11, 10),
				MoveTargets = [targetTile]
			};
			var testee = CreateSmartExecutor(PathStepResult.Success(11, 10));

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.Continue, actual);
			Assert.Equal(Point.Empty, unit.Goto);
		}

		[Fact]
		public void TryExecute_WhenMoveSucceeds_ReturnsTurnComplete()
		{
			// Arrange
			ITile targetTile = new MockedGrassland(11, 10);
			TestUnit unit = new()
			{
				Owner = 1,
				X = 10,
				Y = 10,
				Goto = new Point(11, 10),
				MoveTargets = [targetTile],
				MoveToHandler = (_, _) => true
			};
			var testee = CreateSmartExecutor(PathStepResult.Success(11, 10));

			// Act
			AiGotoExecutionResult actual = testee.TryExecute(unit);

			// Assert
			Assert.Equal(AiGotoExecutionResult.TurnComplete, actual);
			Assert.Equal(1, unit.LastMoveRelX);
			Assert.Equal(0, unit.LastMoveRelY);
		}

		private static SmartAiGotoExecutor CreateSmartExecutor(PathStepResult result)
		{
			var pathfinder = new StubPathfinder(result);
			var pathfinderFactory = new StubPathfinderFactory(pathfinder);
			var randomService = new StubRandomService();
			return new SmartAiGotoExecutor(pathfinderFactory, randomService);
		}

		private sealed class StubExecutor : IAiGotoExecutor
		{
			private readonly AiGotoExecutionResult _result;

			public StubExecutor(AiGotoExecutionResult result)
			{
				_result = result;
			}

			public AiGotoExecutionResult TryExecute(IUnit unit) => _result;
		}

		private sealed class StubPathfinderFactory : IPathfinderFactory
		{
			private readonly IPathfinder _pathfinder;

			public StubPathfinderFactory(IPathfinder pathfinder)
			{
				_pathfinder = pathfinder;
			}

			public IPathfinder CreateFor(IUnit unit) => _pathfinder;
		}

		private sealed class StubPathfinder : IPathfinder
		{
			private readonly PathStepResult _result;

			public StubPathfinder(PathStepResult result)
			{
				_result = result;
			}

			public PathStepResult GetNextStep(IUnit unit, Point destination) => _result;
		}

		private sealed class StubRandomService : IRandomService
		{
			public int NextInt(int max) => 0;

			public int NextInt(int min, int max) => min;

			public bool Hit(int percent) => percent > 0;

			public byte NextByte(byte min, byte maxExclusive) => min;

			public byte NextByte(byte maxExclusive) => 0;
		}

		private sealed class TestUnit : MockedIUnit, IUnit
		{
			public Func<int, int, bool> MoveToHandler { get; set; } = (_, _) => true;
			public int LastMoveRelX { get; private set; }
			public int LastMoveRelY { get; private set; }
			public bool SkipTurnCalled { get; private set; }

			bool IUnit.MoveTo(int relX, int relY)
			{
				LastMoveRelX = relX;
				LastMoveRelY = relY;
				return MoveToHandler(relX, relY);
			}

			void IUnit.SkipTurn()
			{
				SkipTurnCalled = true;
			}
		}
	}
}
