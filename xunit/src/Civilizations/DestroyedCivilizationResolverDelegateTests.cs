using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests.Civilizations
{
	/// <summary>
	/// Covers how a finished game is asked which civilization held a player slot when it was destroyed.
	/// </summary>
	public class DestroyedCivilizationResolverDelegateTests : IDisposable
	{
		private const short Seed = 12345;
		private const int Competition = 12;
		private const byte HumanPlayerIndex = 2;

		private readonly MockRuntime _runtime;

		public DestroyedCivilizationResolverDelegateTests()
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

		private static IReadOnlyList<DestroyedCivilizationEntry> Resolve(IReadOnlyList<ReplayData> replay)
			=> new DestroyedCivilizationResolverDelegate()
				.Resolve(replay, Seed, Competition, HumanPlayerIndex, HumanCivilization());

		/// <summary>
		/// Looks up the result for a single replay entry, for tests that only care about one destruction.
		/// </summary>
		private static DestroyedCivilizationEntry ResultFor(
			IReadOnlyList<DestroyedCivilizationEntry> resolved, ReplayData.CivilizationDestroyed destroyed)
			=> resolved.Single(entry => entry.Destroyed == destroyed);

		[Fact]
		public void WithoutRespawnsTheStartingCivilizationIsReported()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());
			ReplayData.CivilizationDestroyed destroyed = new(40, 9, HumanPlayerIndex);

			DestroyedCivilizationEntry resolved = ResultFor(Resolve([destroyed]), destroyed);

			Assert.Equal(assignment[9].Id, resolved.Civilization.Id);
		}

		[Fact]
		public void AfterARespawnTheSlotReportsTheNewCivilization()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());
			ICivilization replacement = Common.Civilizations.First(civ => civ.PreferredPlayerNumber != 0 && civ.Id != assignment[9].Id);

			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationRespawned respawn = new(40, 9, (byte)replacement.Id);
			ReplayData.CivilizationDestroyed second = new(70, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([first, respawn, second]);

			// The first destruction still refers to the civilization that started on the slot ...
			Assert.Equal(assignment[9].Id, ResultFor(resolved, first).Civilization.Id);
			// ... and the second one to the civilization that took it over.
			Assert.Equal(replacement.Id, ResultFor(resolved, second).Civilization.Id);
		}

		[Fact]
		public void ARespawnOnAnotherSlotDoesNotShiftTheResult()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());
			ICivilization replacement = Common.Civilizations.First(civ => civ.PreferredPlayerNumber != 0 && civ.Id != assignment[5].Id);

			// Slot 5 is destroyed by someone else and respawns; slot 9 must be unaffected.
			ReplayData.CivilizationDestroyed other = new(30, 5, 4);
			ReplayData.CivilizationRespawned respawn = new(30, 5, (byte)replacement.Id);
			ReplayData.CivilizationDestroyed mine = new(60, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([other, respawn, mine]);

			Assert.Equal(assignment[9].Id, ResultFor(resolved, mine).Civilization.Id);
		}

		[Fact]
		public void ASharedStartingCivilizationKeepsItsNumber()
		{
			// 20 players share the 14 civilizations, so at least one civilization is assigned twice. The
			// second slot that got it has to report occurrence 1, which is what the Roman numeral is built from.
			const int sharedCompetition = 20;
			CivilizationAssignment assignment =
				CivilizationAssignment.Create(Seed, sharedCompetition, HumanPlayerIndex, HumanCivilization());

			int repeatedSlot = Enumerable.Range(1, sharedCompetition)
				.First(slot => Enumerable.Range(1, slot - 1).Any(earlier => assignment[earlier].Id == assignment[slot].Id));

			ReplayData.CivilizationDestroyed destroyed = new(40, (byte)repeatedSlot, HumanPlayerIndex);
			ReplayData.CivilizationRespawned unrelatedRespawn = new(41, 1, (byte)assignment[1].Id);

			DestroyedCivilizationEntry resolved = ResultFor(
				new DestroyedCivilizationResolverDelegate()
					.Resolve([destroyed, unrelatedRespawn], Seed, sharedCompetition, HumanPlayerIndex, HumanCivilization()),
				destroyed);

			Assert.Equal(assignment[repeatedSlot].Id, resolved.Civilization.Id);
			Assert.True(resolved.Occurrence > 0);
		}

		[Fact]
		public void ARespawnIntoAFreeCivilizationIsNotNumbered()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());
			int[] usedIds = [.. Enumerable.Range(0, assignment.Count).Select(slot => assignment[slot].Id)];
			ICivilization free = Common.Civilizations.First(civ => civ.PreferredPlayerNumber != 0 && !usedIds.Contains(civ.Id));

			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationRespawned respawn = new(40, 9, (byte)free.Id);
			ReplayData.CivilizationDestroyed second = new(70, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([first, respawn, second]);

			Assert.Equal(0, ResultFor(resolved, second).Occurrence);
		}

		[Fact]
		public void ARespawnIntoATakenCivilizationIsNumbered()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());

			// Slot 9 comes back as the civilization slot 5 is still playing.
			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationRespawned respawn = new(40, 9, (byte)assignment[5].Id);
			ReplayData.CivilizationDestroyed second = new(70, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([first, respawn, second]);

			Assert.Equal(assignment[5].Id, ResultFor(resolved, second).Civilization.Id);
			Assert.Equal(1, ResultFor(resolved, second).Occurrence);
		}

		[Fact]
		public void ADeadPlayerDoesNotCountTowardsTheNumber()
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(Seed, Competition, HumanPlayerIndex, HumanCivilization());

			// Slot 5 is destroyed first, so slot 9 taking its civilization afterwards is the only one using it.
			ReplayData.CivilizationDestroyed other = new(20, 5, HumanPlayerIndex);
			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationRespawned respawn = new(40, 9, (byte)assignment[5].Id);
			ReplayData.CivilizationDestroyed second = new(70, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([other, first, respawn, second]);

			Assert.Equal(0, ResultFor(resolved, second).Occurrence);
		}

		[Fact]
		public void ResultsKeepTheReplayOrderWithinOneTurn()
		{
			// Three slots destroyed on the same turn: ordering by turn alone would leave the sequence open,
			// so the result has to follow the order the entries appear in the replay.
			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationDestroyed second = new(40, 5, HumanPlayerIndex);
			ReplayData.CivilizationDestroyed third = new(40, 3, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([first, second, third]);

			Assert.Equal([first, second, third], resolved.Select(entry => entry.Destroyed));
		}

		[Fact]
		public void LegacySavesWithoutRespawnEntriesUseTheBuddySupplier()
		{
			// No CivilizationRespawned entries at all: the result has to match what the seed-based
			// reconstruction produced before those entries existed.
			BaseCivilization.BuddyCivilization supplier =
				BaseCivilization.GetBuddyCivilizationSupplier(Seed, Competition, HumanPlayerIndex, HumanCivilization());
			ICivilization expectedFirst = supplier(9);
			ICivilization expectedSecond = supplier(9);

			ReplayData.CivilizationDestroyed first = new(40, 9, HumanPlayerIndex);
			ReplayData.CivilizationDestroyed second = new(70, 9, HumanPlayerIndex);

			IReadOnlyList<DestroyedCivilizationEntry> resolved = Resolve([first, second]);

			Assert.Equal(expectedFirst.Id, ResultFor(resolved, first).Civilization.Id);
			Assert.Equal(expectedSecond.Id, ResultFor(resolved, second).Civilization.Id);
		}
	}
}
