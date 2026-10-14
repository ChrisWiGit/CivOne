using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CivOne.Services;
using CivOne.Services.HallOfFame;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Unit tests for <see cref="HallOfFamePersistService"/> persistence behaviour.
	/// </summary>
	public sealed class HallOfFamePersistServiceTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _hallOfFameFile;
		private readonly HallOfFamePersistService? _testee;

		public HallOfFamePersistServiceTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_hallOfFameFile = Path.Combine(_storageDirectory, "HallOfFame.yaml");
			IHallOfFameFileRepository repository = new HallOfFameFileRepositoryImpl(new AtomicFileReplacementService());
			_testee = new HallOfFamePersistService(repository);
		}

		[Fact]
		public void ViewEntriesWhenFileMissingLogsInfoAndDoesNotCreateFile()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis
			// Arrange
			string? logged = null;

			// Act
			var actual = _testee.ViewEntries(_storageDirectory, message => logged = message);

			// Assert
			Assert.Empty(actual);
			Assert.Contains("missing", logged, StringComparison.OrdinalIgnoreCase);
			Assert.False(File.Exists(_hallOfFameFile));
		}

		[Fact]
		public void AddEntryWhenFileMissingCreatesFileAndPersistsEntry()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis

			// Arrange
			HallOfFameEntry expectedEntry = CreateEntry("Leader A", score: 100);

			// Act
			var actual = _testee.AddEntry(_storageDirectory, expectedEntry);

			// Assert
			Assert.Single(actual);
			Assert.True(File.Exists(_hallOfFameFile));
			Assert.Equal(expectedEntry.LeaderName, actual[0].LeaderName);
			Assert.Equal(expectedEntry.Score, actual[0].Score);
		}

		[Fact]
		public void AddEntryWhenMoreThanFiveEntriesKeepsTopFiveByRating()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis

			// Arrange
			for (int i = 0; i < 6; i++)
			{
				_testee.AddEntry(_storageDirectory, CreateEntry($"Leader {i}", i * 10));
			}

			// Act
			var actual = _testee.ViewEntries(_storageDirectory);

			// Assert
			Assert.Equal(5, actual.Count);
			Assert.Equal(50, actual[0].Score);
			Assert.Equal(10, actual[^1].Score);
			Assert.DoesNotContain(actual, entry => entry.Score == 0);
			Assert.True(actual.SequenceEqual(actual.OrderByDescending(entry => entry.RatingPercent)));
		}

		[Fact]
		public void AddEntryWhenRatingOnlyEqualsLowestKeptEntryIsNotAdded()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis

			// Arrange
			for (int i = 1; i <= 5; i++)
			{
				_testee.AddEntry(_storageDirectory, CreateEntry($"Leader {i}", i * 10));
			}

			// Act: same rating as the lowest kept entry, but a higher score.
			var actual = _testee.AddEntry(_storageDirectory, CreateEntry("Tie", score: 999, ratingPercent: 10));

			// Assert
			Assert.DoesNotContain(actual, entry => entry.LeaderName == "Tie");
		}

		[Fact]
		public void AddEntryWhenRatingHigherButScoreLowerIsRankedByRating()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis

			// Arrange
			_testee.AddEntry(_storageDirectory, CreateEntry("High score", score: 500, ratingPercent: 20));

			// Act: a higher difficulty gives a better rating for a lower score.
			var actual = _testee.AddEntry(_storageDirectory, CreateEntry("High rating", score: 100, ratingPercent: 40));

			// Assert
			Assert.Equal("High rating", actual[0].LeaderName);
		}

		[Fact]
		public void AddEntrySerializesCreatedAtUtcAsScalar()
		{
			Assert.NotNull(_testee); // to satisfy nullable reference type analysis

			// Arrange
			HallOfFameEntry expectedEntry = CreateEntry("Leader A", score: 100);

			// Act
			_testee.AddEntry(_storageDirectory, expectedEntry);
			string yaml = File.ReadAllText(_hallOfFameFile);

			// Assert
			Assert.Contains("CreatedAtUtc:", yaml, StringComparison.Ordinal);
			Assert.DoesNotContain("UtcDateTime:", yaml, StringComparison.Ordinal);
			Assert.DoesNotContain("LocalDateTime:", yaml, StringComparison.Ordinal);
			Assert.Matches(new Regex(@"CreatedAtUtc:\s+\d{4}-\d{2}-\d{2}T.*Z", RegexOptions.CultureInvariant), yaml);
		}

		public void Dispose()
		{
			if (Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}

		private static HallOfFameEntry CreateEntry(string leaderName, int score, int? ratingPercent = null)
		{
			return new HallOfFameEntry(
				LeaderName: leaderName,
				LeaderTitle: "King",
				CivilizationNamePlural: "Romans",
				YearLabel: "1 AD",
				Population: 1234,
				Score: score,
				RatingRankLabel: "Warlord",
				RatingPercent: ratingPercent ?? score,
				CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(score));
		}
	}
}
