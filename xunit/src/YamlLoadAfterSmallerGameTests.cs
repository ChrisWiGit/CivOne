using System;
using System.IO;
using System.Linq;
using CivOne.Persistence.Factories;
using CivOne.Services;
using CivOne.src;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Regression test for loading a save with many players while a game with fewer players is still loaded.
	///
	/// City hydration resolves the city owner by player number. It used to do so through the
	/// <see cref="Game"/> singleton, which still points at the previously loaded game while the new save
	/// is being mapped. Loading a 20-player save after an 5-player game therefore looked up player numbers
	/// beyond the old game's player array (assert in <c>Game.GetPlayer</c>, then a null owner and a failed load).
	/// </summary>
	public sealed class YamlLoadAfterSmallerGameTests : IDisposable
	{
		private const int LargeCompetition = 20;
		private const int SmallCompetition = 5;

		/// <summary>
		/// A player slot that only exists in the large game, not in the small one.
		/// </summary>
		private const byte HighPlayerIndex = 17;

		private readonly MockRuntime _runtime;
		private readonly string _saveFile;

		public YamlLoadAfterSmallerGameTests()
		{
			TranslationServiceFactory.ResetForTests();

			_runtime = new MockRuntime(new RuntimeSettings { InitialSeed = 24601 });
			_saveFile = Path.Combine(Path.GetTempPath(), $"civone-load-order-{Guid.NewGuid():N}.cos");

			Map.Reset(new MapGenerationFromYaml());
			Map.Instance.LoadEarthMapInThread();
		}

		public void Dispose()
		{
			if (File.Exists(_saveFile))
			{
				File.Delete(_saveFile);
			}

			Map.Reset();
			Game.Wipe();
			_runtime.Dispose();
			RuntimeHandler.Wipe();
			GC.SuppressFinalize(this);
		}

		[Fact]
		public void LoadingALargeSaveWhileASmallerGameIsLoadedKeepsHighPlayerCities()
		{
			Game.CreateGame(3, LargeCompetition, Common.Civilizations.First(c => c.Name == "Chinese"));

			Player highPlayer = Game.Instance.GetPlayer(HighPlayerIndex)!;
			IUnit startUnit = Game.Instance.GetUnits().First(u => u.Owner == HighPlayerIndex);
			City? city = Game.Instance.AddCity(highPlayer, 0, startUnit.X, startUnit.Y);
			Assert.NotNull(city);

			new YamlSaveGameService(Game.Instance).SaveCos(_saveFile);

			// Replace the running game with a smaller one, so the player array of the "current" game is
			// shorter than the one of the save file that is loaded next.
			Game.CreateGame(3, SmallCompetition, Common.Civilizations.First(c => c.Name == "Chinese"), replaceExisting: true);
			Assert.Equal(SmallCompetition + 1, Game.Instance.Players.Count());

			Assert.True(Game.LoadYamlGame(_saveFile), "loading the large save after the smaller game failed");

			Assert.Equal(LargeCompetition + 1, Game.Instance.Players.Count());
			Assert.Contains(Game.Instance.GetCities(), c => c.CityOwnerPlayerIndex == HighPlayerIndex);
		}
	}
}
