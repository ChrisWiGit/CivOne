using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public class CivilizationAssignmentTests : IDisposable
	{
		private readonly MockRuntime _runtime;
		private const short Seed = 12345;

		public CivilizationAssignmentTests()
		{
			TranslationServiceFactory.ResetForTests();
			_runtime = new MockRuntime(new RuntimeSettings());
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_runtime.Dispose();
				RuntimeHandler.Wipe();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private static ICivilization HumanCivilization() => Common.Civilizations.First(c => c.Name == "Babylonian");

		[Fact]
		public void CreateIsDeterministicForTheSameSeed()
		{
			ICivilization human = HumanCivilization();

			CivilizationAssignment first = CivilizationAssignment.Create(Seed, 20, human.PreferredPlayerNumber, human);
			CivilizationAssignment second = CivilizationAssignment.Create(Seed, 20, human.PreferredPlayerNumber, human);

			for (int i = 0; i <= 20; i++)
			{
				Assert.Equal(first[i].Id, second[i].Id);
			}
		}

		[Fact]
		public void CreateMatchesTheOriginalPreferredPlayerNumberAlgorithmForUpToSevenCompetitors()
		{
			// For competition <= 7, every non-barbarian, non-human slot i must be filled by a civilization
			// whose PreferredPlayerNumber == i, exactly like the pre-existing algorithm this replaces.
			ICivilization human = HumanCivilization();
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, 7, human.PreferredPlayerNumber, human);

			for (int i = 0; i <= 7; i++)
			{
				if (i == human.PreferredPlayerNumber)
				{
					Assert.Equal(human.Id, assignment[i].Id);
					continue;
				}

				Assert.Equal(i, assignment[i].PreferredPlayerNumber);
			}
		}

		[Fact]
		public void AllFourteenCivilizationsAreUsedBeforeAnyRepeats()
		{
			ICivilization human = HumanCivilization();
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, 20, human.PreferredPlayerNumber, human);

			int[] firstFourteenIds = [.. Enumerable.Range(1, 14).Select(i => assignment[i].Id)];
			Assert.Equal(14, firstFourteenIds.Distinct().Count());
			Assert.Equal([.. Enumerable.Range(1, 14)], firstFourteenIds.OrderBy(id => id));

			// With only 14 non-barbarian civilizations and 20 competitors, slot 15 must necessarily
			// repeat one of the civilizations already used in slots 1-14 (pigeonhole principle).
			Assert.Contains(assignment[15].Id, firstFourteenIds);
		}

		[Fact]
		public void NoPlayerSlotIsGivenTheBarbarians()
		{
			// The barbarians carry Id 15, not 0, so filtering the reuse pool by Id alone let them through
			// once the pool was refilled - which happens from slot 15 onwards.
			ICivilization human = HumanCivilization();
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, 31, human.PreferredPlayerNumber, human);

			for (int slot = 1; slot < assignment.Count; slot++)
			{
				Assert.NotEqual(0, assignment[slot].PreferredPlayerNumber);
			}
		}

		[Fact]
		public void BuddySwapsToTheOtherCivilizationInThePair()
		{
			ICivilization human = HumanCivilization();
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, 7, human.PreferredPlayerNumber, human);

			int slot = Enumerable.Range(1, 7).First(i => i != human.PreferredPlayerNumber);
			ICivilization original = assignment[slot];
			ICivilization buddy = assignment.Buddy(slot);

			Assert.NotEqual(original.Id, buddy.Id);
			Assert.Equal(original.PreferredPlayerNumber, buddy.PreferredPlayerNumber);
			int expectedBuddyId = original.Id >= 8 ? original.Id - 7 : original.Id + 7;
			Assert.Equal(expectedBuddyId, buddy.Id);
		}

		[Fact]
		public void BuddyCivilizationSupplierAlternatesBetweenBuddiesOnRepeatedCalls()
		{
			ICivilization human = HumanCivilization();
			int slot = Enumerable.Range(1, 7).First(i => i != human.PreferredPlayerNumber);

			BaseCivilization.BuddyCivilization supplier = BaseCivilization.GetBuddyCivilizationSupplier(Seed, 7, human.PreferredPlayerNumber, human);

			int firstId = supplier(slot).Id;
			int secondId = supplier(slot).Id;
			int thirdId = supplier(slot).Id;

			Assert.NotEqual(firstId, secondId);
			Assert.Equal(firstId, thirdId);
		}
	}
}
