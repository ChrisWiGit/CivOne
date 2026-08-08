using System.Linq;
using CivOne.Civilizations;
using CivOne.Services.Civilizations;
using CivOne.Services.StartPositions;
using Xunit;

namespace CivOne.UnitTests.Services.Civilizations
{
	/// <summary>
	/// Covers the civilization a destroyed player slot respawns into.
	/// Runs on stand-in civilizations that mirror the real Id layout (1-14 in buddy pairs seven apart,
	/// barbarians as Id 15 with preferred player number 0), so no running game is needed.
	/// </summary>
	public class RespawnCivilizationServiceTests
	{
		private const int BarbarianId = 15;

		private static ICivilization[] Civilizations()
			=> [.. Enumerable.Range(1, 14)
					.Select(id => (ICivilization)new MockedICivilization(1, (byte)id) { PreferredPlayerNumber = (byte)(((id - 1) % 7) + 1) })
					.Append(new MockedICivilization(1, BarbarianId) { PreferredPlayerNumber = 0 })];

		private static ICivilization Civilization(ICivilization[] all, int id) => all.First(civ => civ.Id == id);

		[Fact]
		public void PicksTheBuddyCivilizationWhenItIsFree()
		{
			ICivilization[] all = Civilizations();
			StubRandomService random = new();
			RespawnCivilizationService testee = new(all, random);

			// Romans (1) and Russians (8) are a buddy pair.
			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 1), [Civilization(all, 2)], true);

			Assert.Equal(8, result.Civilization.Id);
			Assert.Equal(0, result.Occurrence);
			Assert.Empty(random.RequestedRanges);
		}

		[Fact]
		public void DoesNotPreferTheBuddyCivilizationForExtendedPlayerCounts()
		{
			ICivilization[] all = Civilizations();
			RespawnCivilizationService testee = new(all, new StubRandomService());

			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 1), [], false);

			Assert.Equal(2, result.Civilization.Id);
			Assert.NotEqual(8, result.Civilization.Id);
			Assert.Equal(0, result.Occurrence);
		}

		[Fact]
		public void PicksAFreeCivilizationWhenTheBuddyIsTaken()
		{
			ICivilization[] all = Civilizations();
			RespawnCivilizationService testee = new(all, new StubRandomService());

			ICivilization[] inUse = [Civilization(all, 8)];
			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 1), inUse, true);

			Assert.NotEqual(1, result.Civilization.Id);
			Assert.NotEqual(8, result.Civilization.Id);
			Assert.Equal(0, result.Occurrence);
		}

		[Fact]
		public void NeverPicksTheDestroyedCivilizationAgain()
		{
			ICivilization[] all = Civilizations();
			RespawnCivilizationService testee = new(all, new StubRandomService());

			// Everything except the destroyed civilization itself is taken.
			ICivilization[] inUse = [.. all.Where(civ => civ.PreferredPlayerNumber != 0 && civ.Id != 3)];
			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 3), inUse, true);

			Assert.NotEqual(3, result.Civilization.Id);
		}

		[Fact]
		public void NeverPicksTheBarbarians()
		{
			ICivilization[] all = Civilizations();
			ICivilization[] inUse = [.. all.Where(civ => civ.PreferredPlayerNumber != 0 && civ.Id != 5)];

			// Repeat, so a random pick cannot pass by luck.
			for (int seed = 0; seed < 20; seed++)
			{
				RespawnCivilizationService testee = new(all, new StubRandomService(seed));
				RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 5), inUse, true);

				Assert.NotEqual(BarbarianId, result.Civilization.Id);
			}
		}

		[Fact]
		public void SharesTheLeastUsedCivilizationWhenEveryOneIsTaken()
		{
			ICivilization[] all = Civilizations();
			RespawnCivilizationService testee = new(all, new StubRandomService());

			// Every civilization is used once, civilization 2 twice: it must not be the one handed out.
			ICivilization[] inUse = [.. all.Where(civ => civ.PreferredPlayerNumber != 0).Append(Civilization(all, 2))];
			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 1), inUse, true);

			Assert.NotEqual(2, result.Civilization.Id);
			Assert.Equal(1, result.Occurrence);
		}

		[Fact]
		public void ReportsHowOftenTheChosenCivilizationIsAlreadyPlayed()
		{
			ICivilization[] all = Civilizations();
			RespawnCivilizationService testee = new(all, new StubRandomService());

			// Two players per civilization, so whatever is picked is the third user.
			ICivilization[] inUse =
			[
				.. all.Where(civ => civ.PreferredPlayerNumber != 0),
				.. all.Where(civ => civ.PreferredPlayerNumber != 0)
			];
			RespawnCivilizationResult result = testee.SelectReplacement(Civilization(all, 1), inUse, true);

			Assert.Equal(2, result.Occurrence);
		}
	}
}
