using CivOne.Enums;
using CivOne.Services.Palace;
using Xunit;

namespace CivOne.UnitTests.Services.Palace
{
	public class HumanCivScorePalaceTriggerTests
	{
		private readonly HumanCivScorePalaceTrigger _trigger = new();

		[Theory]
		// n = upgrades + 1; threshold = (7 - D) * n * (n + 1) / 2 / 2 + 1
		[InlineData(0, 0, 4)]
		[InlineData(0, 3, 3)]
		[InlineData(0, 5, 2)]
		[InlineData(1, 0, 11)]
		[InlineData(1, 3, 7)]
		[InlineData(1, 5, 4)]
		[InlineData(2, 0, 22)]
		[InlineData(2, 3, 13)]
		[InlineData(2, 4, 10)]
		[InlineData(2, 5, 7)]
		public void ThresholdFollowsOriginalFormula(int upgrades, int difficulty, int threshold)
		{
			Assert.True(_trigger.ShouldTrigger(new FakeState { Score = threshold, Upgrades = upgrades, Difficulty = difficulty }));
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = threshold - 1, Upgrades = upgrades, Difficulty = difficulty }));
		}

		[Fact]
		public void AiPlayerNeverTriggers()
		{
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 1_000, IsHuman = false }));
		}

		[Fact]
		public void PalaceOptionOffDoesNotTrigger()
		{
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 1_000, PalaceOptionEnabled = false }));
		}

		[Fact]
		public void ScoringCompletedDoesNotTrigger()
		{
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 1_000, ScoringCompleted = true }));
		}

		[Fact]
		public void NoPalaceCityDoesNotTrigger()
		{
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 1_000, HasPalaceCity = false }));
		}

		[Fact]
		public void FullyUpgradedPalaceDoesNotTrigger()
		{
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 30_000, CanUpgrade = false }));
		}

		[Fact]
		public void NullStateOrPalaceDoesNotTrigger()
		{
			Assert.False(_trigger.ShouldTrigger(null!));
			Assert.False(_trigger.ShouldTrigger(new FakeState { Score = 1_000, NoPalace = true }));
		}

		private sealed class FakeState : IPlayerGameState
		{
			public int Score { get; init; }
			public int Upgrades { get; init; }
			public bool CanUpgrade { get; init; } = true;
			public bool NoPalace { get; init; }

			public int CivilizationScore => Score;
			public IPalaceData Palace => NoPalace ? null! : new FakePalace(Upgrades, CanUpgrade);
			public bool IsHuman { get; init; } = true;
			public int Difficulty { get; init; } = 3;
			public bool PalaceOptionEnabled { get; init; } = true;
			public bool ScoringCompleted { get; init; }
			public bool HasPalaceCity { get; init; } = true;
		}

		private sealed class FakePalace(int upgradeCount, bool canUpgrade) : IPalaceData
		{
			public int PalaceLeft => 0;
			public int PalaceRight => 0;
			public PalaceStyle GetPalaceStyle(int index) => default;
			public byte GetPalaceLevel(int index) => 0;
			public byte GetGardenLevel(int index) => 0;
			public int UpgradeCount => upgradeCount;
			public bool CanUpgrade => canUpgrade;
			public bool IsSlotUnlocked(int index) => false;
		}
	}
}
