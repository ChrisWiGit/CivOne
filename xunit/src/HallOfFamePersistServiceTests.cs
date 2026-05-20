using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CivOne.Services;
using CivOne.Services.HallOfFame;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class HallOfFamePersistServiceTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _hallOfFameFile;
		private readonly IHallOfFamePersistService _testee;

		public HallOfFamePersistServiceTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_hallOfFameFile = Path.Combine(_storageDirectory, "HallOfFame.yaml");
			IHallOfFameFileRepository repository = new HallOfFameFileRepositoryImpl(new AtomicFileReplacementService());
			_testee = new HallOfFamePersistService(repository);
		}

		[Fact]
		public void ViewEntries_WhenFileMissing_LogsInfoAndDoesNotCreateFile()
		{
			// Arrange
			string logged = null;

			// Act
			var actual = _testee.ViewEntries(_storageDirectory, message => logged = message);

			// Assert
			Assert.Empty(actual);
			Assert.Contains("missing", logged, StringComparison.OrdinalIgnoreCase);
			Assert.False(File.Exists(_hallOfFameFile));
		}

		[Fact]
		public void AddEntry_WhenFileMissing_CreatesFileAndPersistsEntry()
		{
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
		public void AddEntry_WhenMoreThanFiveEntries_KeepsTopFiveByScore()
		{
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
			Assert.True(actual.SequenceEqual(actual.OrderByDescending(entry => entry.Score)));
		}

		[Fact]
		public void AddEntry_SerializesCreatedAtUtcAsScalar()
		{
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
	}
}
