using CivOne.Services.Peace;
using Xunit;

namespace CivOne.UnitTests.Services.Peace
{
	public class PeaceTurnServiceTests
	{
		[Fact]
		public void AdvanceTurnInAdYearIncrements()
		{
			PeaceTurnService service = new();
			service.Restore(3);

			service.AdvanceTurn(1500);

			Assert.Equal(4, service.PeaceTurns);
		}

		[Theory]
		[InlineData(-4000)]
		[InlineData(-1)]
		[InlineData(0)]
		public void AdvanceTurnInBcYearDoesNotIncrement(int year)
		{
			PeaceTurnService service = new();
			service.Restore(3);

			service.AdvanceTurn(year);

			Assert.Equal(3, service.PeaceTurns);
		}

		[Fact]
		public void CombatResetsAndTheSameWrapCountsAgain()
		{
			PeaceTurnService service = new();
			service.Restore(10);
			service.RegisterCombat(1, 2);

			service.AdvanceTurn(1500);

			Assert.Equal(1, service.PeaceTurns);
		}

		[Fact]
		public void CombatInBcYearResetsToZero()
		{
			PeaceTurnService service = new();
			service.Restore(10);
			service.RegisterCombat(1, 2);

			service.AdvanceTurn(-1000);

			Assert.Equal(0, service.PeaceTurns);
		}

		[Fact]
		public void ResetIsDeferredUntilTurnWrap()
		{
			PeaceTurnService service = new();
			service.Restore(10);

			service.RegisterCombat(1, 2);

			Assert.Equal(10, service.PeaceTurns);
		}

		[Theory]
		[InlineData(0, 2)]
		[InlineData(2, 0)]
		public void CombatWithBarbariansKeepsStreak(int attacker, int defender)
		{
			PeaceTurnService service = new();
			service.Restore(10);
			service.RegisterCombat(attacker, defender);

			service.AdvanceTurn(1500);

			Assert.Equal(11, service.PeaceTurns);
		}

		[Fact]
		public void HostileActionWithoutDefenderResets()
		{
			PeaceTurnService service = new();
			service.Restore(10);
			service.RegisterCombat(1, -1);

			service.AdvanceTurn(1500);

			Assert.Equal(1, service.PeaceTurns);
		}

		[Fact]
		public void RestoreDropsPendingReset()
		{
			PeaceTurnService service = new();
			service.RegisterCombat(1, 2);

			service.Restore(10);
			service.AdvanceTurn(1500);

			Assert.Equal(11, service.PeaceTurns);
		}

		[Fact]
		public void CounterIsNotCappedBelowUshortMax()
		{
			PeaceTurnService service = new();
			service.Restore(200);

			service.AdvanceTurn(1500);

			Assert.Equal(201, service.PeaceTurns);
		}

		[Fact]
		public void CounterSaturatesAtUshortMax()
		{
			PeaceTurnService service = new();
			service.Restore(ushort.MaxValue);

			service.AdvanceTurn(1500);

			Assert.Equal(ushort.MaxValue, service.PeaceTurns);
		}
	}
}
