using System;
using System.IO;
using CivOne.Services;
using CivOne.Services.HallOfFame;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Unit tests for <see cref="HallOfFameCommandService"/> behaviour.
	/// </summary>
	public sealed class HallOfFameCommandServiceTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly IHallOfFameFileRepository _repository;
		private readonly IHallOfFamePersistService _persistService;

		public HallOfFameCommandServiceTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_repository = new HallOfFameFileRepositoryImpl(new AtomicFileReplacementService());
			_persistService = new HallOfFamePersistService(_repository);
		}

		[Fact]
		public void ClearWhenEntriesExistReplacesWithCurrentHumanEntryOnly()
		{
			// Arrange
			_persistService.AddEntry(_storageDirectory, CreateEntry("Old Leader", score: 999));
			HallOfFameEntry currentHumanEntry = CreateEntry("Current Human", score: 123);
			IHallOfFameEntryComposerService composer = new StubComposer(currentHumanEntry);
			IHallOfFameCommandService testee = new HallOfFameCommandService(
				storageDirectory: _storageDirectory,
				persistService: _persistService,
				fileRepository: _repository,
				entryComposerService: composer);

			// Act
			var actual = testee.Clear();

			// Assert
			Assert.Single(actual);
			Assert.Equal("Current Human", actual[0].LeaderName);
			Assert.Equal(123, actual[0].Score);
		}

		[Fact]
		public void ClearPersistsCurrentHumanEntryToFile()
		{
			// Arrange
			HallOfFameEntry currentHumanEntry = CreateEntry("Composer Human", score: 321);
			IHallOfFameEntryComposerService composer = new StubComposer(currentHumanEntry);
			IHallOfFameCommandService testee = new HallOfFameCommandService(
				storageDirectory: _storageDirectory,
				persistService: _persistService,
				fileRepository: _repository,
				entryComposerService: composer);

			// Act
			testee.Clear();
			var reloaded = _persistService.ViewEntries(_storageDirectory);

			// Assert
			Assert.Single(reloaded);
			Assert.Equal("Composer Human", reloaded[0].LeaderName);
			Assert.Equal(321, reloaded[0].Score);
		}

		[Fact]
		public void ClearWhenComposerThrowsInvalidOperationClearsToEmptyList()
		{
			// Arrange
			_persistService.AddEntry(_storageDirectory, CreateEntry("Old Leader", score: 999));
			IHallOfFameEntryComposerService composer = new ThrowingComposer();
			IHallOfFameCommandService testee = new HallOfFameCommandService(
				storageDirectory: _storageDirectory,
				persistService: _persistService,
				fileRepository: _repository,
				entryComposerService: composer);

			// Act
			var actual = testee.Clear();

			// Assert
			Assert.Empty(actual);
			var reloaded = _persistService.ViewEntries(_storageDirectory);
			Assert.Empty(reloaded);
		}

		public void Dispose()
		{
			if (Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}

		private static HallOfFameEntry CreateEntry(string leaderName, int score)
		{
			return new HallOfFameEntry(
				LeaderName: leaderName,
				LeaderTitle: "King",
				CivilizationNamePlural: "Romans",
				YearLabel: "1 AD",
				Population: 1234,
				Score: score,
				RatingRankLabel: "Warlord",
				RatingPercent: 20,
				CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(score));
		}

		private sealed class StubComposer(HallOfFameEntry entry) : IHallOfFameEntryComposerService
		{
			private readonly HallOfFameEntry _entry = entry;

			public HallOfFameEntry ComposeForHuman() => _entry;
		}

		private sealed class ThrowingComposer : IHallOfFameEntryComposerService
		{
			public HallOfFameEntry ComposeForHuman()
			{
				throw new InvalidOperationException("Simulated missing game context.");
			}
		}
	}
}
