using System;
using Xunit;
using CivOne.Services;

namespace CivOne.src
{
	public class SveSaveCompatibilityServiceTests
	{
		private static SveSaveCompatibilitySnapshot CreateSnapshot(
			bool isLoadedFromYaml = false,
			int playerCount = 8,
			int mapWidth = 80,
			int mapHeight = 50,
			int cityCount = 1,
			int[] tradeCityCountsPerCity = null,
			byte[] cityOwners = null,
			byte[] unitOwners = null)
		{
			return SveSaveCompatibilitySnapshot.Builder()
				.FromYamlSource(isLoadedFromYaml)
				.WithPlayerCount(playerCount)
				.WithMapSize(mapWidth, mapHeight)
				.WithCityCount(cityCount)
				.WithTradeCityCountsPerCity(tradeCityCountsPerCity ?? [0])
				.WithCityOwners(cityOwners ?? [1])
				.WithUnitOwners(unitOwners ?? [1])
				.Build();
		}

		[Fact]
		public void Evaluate_WhenYamlLoaded_ReturnsIncompatible()
		{
			var testee = new SveSaveCompatibilityService();
			var snapshot = CreateSnapshot(isLoadedFromYaml: true);

			var actual = testee.Evaluate(snapshot);

			Assert.False(actual.CanSaveAsSve);
			Assert.Contains("YAML/COS", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Evaluate_WhenMoreThanEightPlayers_ReturnsIncompatible()
		{
			var testee = new SveSaveCompatibilityService();
			var snapshot = CreateSnapshot(playerCount: 9);

			var actual = testee.Evaluate(snapshot);

			Assert.False(actual.CanSaveAsSve);
			Assert.Contains("at most 8 players", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Evaluate_WhenMapIsNot80x50_ReturnsIncompatible()
		{
			var testee = new SveSaveCompatibilityService();
			var snapshot = CreateSnapshot(mapWidth: 81, mapHeight: 50);

			var actual = testee.Evaluate(snapshot);

			Assert.False(actual.CanSaveAsSve);
			Assert.Contains("80x50", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Evaluate_WhenCityHasMoreThanThreeTradeCities_ReturnsIncompatible()
		{
			var testee = new SveSaveCompatibilityService();
			var snapshot = CreateSnapshot(tradeCityCountsPerCity: [4]);

			var actual = testee.Evaluate(snapshot);

			Assert.False(actual.CanSaveAsSve);
			Assert.Contains("at most 3 trade cities", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Evaluate_WhenStateFitsSveLimits_ReturnsCompatible()
		{
			var testee = new SveSaveCompatibilityService();
			var snapshot = CreateSnapshot(
				isLoadedFromYaml: false,
				playerCount: 8,
				mapWidth: 80,
				mapHeight: 50,
				cityCount: 8,
				cityOwners: [0, 1, 2, 3],
				unitOwners: [0, 1, 1, 2, 3, 4, 4]);

			var actual = testee.Evaluate(snapshot);

			Assert.True(actual.CanSaveAsSve);
			Assert.True(string.IsNullOrWhiteSpace(actual.Reason));
		}
	}
}
