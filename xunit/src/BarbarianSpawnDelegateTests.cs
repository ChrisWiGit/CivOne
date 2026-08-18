using CivOne.Civilizations;
using CivOne.Enums;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the decision whether barbarians appear in a turn and which kind.
	///
	/// The spawn rhythm and the random draw are injected, so the rules can be checked without a running
	/// game.
	/// </summary>
	public class BarbarianSpawnDelegateTests
	{
		private const int LandRoll = 75;
		private const int SeaRoll = 25;

		private static (BarbarianSpawnDelegate Spawn, MockedIRandomService Random) CreateDelegate(
			BarbarianActivity activity,
			int roll,
			bool isSpawnTurn = true)
		{
			MockedIRandomService random = new(roll);
			BarbarianSpawnDelegate spawn = new(() => activity, random, () => isSpawnTurn);
			return (spawn, random);
		}

		/// <summary>
		/// Outside a spawn turn nothing happens, and nothing is drawn.
		/// </summary>
		[Fact]
		public void NothingHappensOutsideASpawnTurn()
		{
			(BarbarianSpawnDelegate spawn, MockedIRandomService random) =
				CreateDelegate(BarbarianActivity.VillagesAndRaids, LandRoll, isSpawnTurn: false);

			Assert.Equal(BarbarianSpawnKind.None, spawn.GetSpawnKind());
			Assert.Equal(0, random.DrawCount);
		}

		/// <summary>
		/// With both kinds allowed the draw decides, exactly as before the setting existed.
		/// </summary>
		[Theory]
		[InlineData(LandRoll, BarbarianSpawnKind.Land)]
		[InlineData(SeaRoll, BarbarianSpawnKind.Sea)]
		public void DrawDecidesWhenBothKindsAreAllowed(int roll, BarbarianSpawnKind expected)
		{
			(BarbarianSpawnDelegate spawn, _) = CreateDelegate(BarbarianActivity.VillagesAndRaids, roll);

			Assert.Equal(expected, spawn.GetSpawnKind());
		}

		/// <summary>
		/// A switched-off kind does not hand its turn to the other one.
		/// Otherwise the remaining kind would appear twice as often as in the original game.
		/// </summary>
		[Theory]
		[InlineData(BarbarianActivity.SeaRaids, LandRoll)]
		[InlineData(BarbarianActivity.LandRaids, SeaRoll)]
		public void DisabledKindSpawnsNothingInsteadOfTheOtherKind(BarbarianActivity activity, int roll)
		{
			(BarbarianSpawnDelegate spawn, _) = CreateDelegate(activity, roll);

			Assert.Equal(BarbarianSpawnKind.None, spawn.GetSpawnKind());
		}

		/// <summary>
		/// A single allowed kind still appears when the draw picks it.
		/// </summary>
		[Theory]
		[InlineData(BarbarianActivity.LandRaids, LandRoll, BarbarianSpawnKind.Land)]
		[InlineData(BarbarianActivity.SeaRaids, SeaRoll, BarbarianSpawnKind.Sea)]
		public void SingleAllowedKindAppearsOnItsOwnDraw(BarbarianActivity activity, int roll, BarbarianSpawnKind expected)
		{
			(BarbarianSpawnDelegate spawn, _) = CreateDelegate(activity, roll);

			Assert.Equal(expected, spawn.GetSpawnKind());
		}

		/// <summary>
		/// Villages alone produce no raiding parties.
		/// </summary>
		[Theory]
		[InlineData(LandRoll)]
		[InlineData(SeaRoll)]
		public void VillagesAloneSpawnNoRaidingParties(int roll)
		{
			(BarbarianSpawnDelegate spawn, _) = CreateDelegate(BarbarianActivity.Villages, roll);

			Assert.Equal(BarbarianSpawnKind.None, spawn.GetSpawnKind());
		}

		/// <summary>
		/// A spawn turn always draws once, whatever the setting allows.
		/// This keeps the random sequence of a game independent of the barbarian setting.
		/// </summary>
		[Theory]
		[InlineData(BarbarianActivity.None)]
		[InlineData(BarbarianActivity.Villages)]
		[InlineData(BarbarianActivity.SeaRaids)]
		[InlineData(BarbarianActivity.VillagesAndRaids)]
		public void SpawnTurnAlwaysDrawsOnce(BarbarianActivity activity)
		{
			(BarbarianSpawnDelegate spawn, MockedIRandomService random) = CreateDelegate(activity, LandRoll);

			spawn.GetSpawnKind();

			Assert.Equal(1, random.DrawCount);
		}
	}
}
