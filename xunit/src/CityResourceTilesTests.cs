using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Civilizations;
using CivOne.Units;
using Xunit;

namespace CivOne.src
{
	public class CityResourceTilesTests : TestsBase
	{
		[Fact]
		public void GetResourceTilesRoundTripsInnerAndOuterNorthEastTiles()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			SetResourceTiles(city, [city.Tile[1, -1], city.Tile[2, -1]]);

			byte[] data = city.GetResourceTiles();
			List<ITile> restored = new CityLoadGame().GetResourceTilesFromGameData(city, data);

			Assert.Contains(restored, tile => tile.X == city.X + 1 && tile.Y == city.Y - 1);
			Assert.Contains(restored, tile => tile.X == city.X + 2 && tile.Y == city.Y - 1);
			Assert.Equal(2, restored.Count);
		}

		[Fact]
		public void SizeIncreaseFillsAllMissingResourceTiles()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 4;

			Assert.Equal(city.Size + 1, city.ResourceTiles.Length);
		}

		[Fact]
		public void SizeIncreaseStopsWhenNoMoreTilesAvailable()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 30;

			Assert.True(city.ResourceTiles.Length <= 21);
		}

		[Fact]
		public void SetResourceTilesBreaksWhenNoBestTileExists()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 30;
			SetResourceTiles(city, []);

			InvokePrivateSetResourceTiles(city);

			// This map position exposes only 8 workable surrounding tiles.
			// When those are filled, bestTile becomes null and the loop must stop.
			Assert.Equal(9, city.ResourceTiles.Length);
		}

		[Fact]
		public void BarbarianCityCreationKeepsWorkedTilesAndNoSpecialistsAtSizeOne()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			Player? barbarian = Game.Instance.GetPlayer(0);
			Assert.NotNull(barbarian);
			City? city = Game.Instance.AddCity(barbarian, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			Assert.Contains(city.ResourceTiles, tile => tile.X == city.X && tile.Y == city.Y);
			Assert.True(city.ResourceTiles.Length >= 2);
			Assert.Empty(city.Specialists);
		}

		[Fact]
		public void FoundCityOrderForComputerOwnerKeepsWorkedTilesAndNoSpecialistsAtSizeOne()
		{
			Player computerPlayer = Game.Instance.Players.First(player => player != null && !player.IsHuman && player.Civilization is not Barbarian);
			byte computerPlayerId = Game.Instance.PlayerNumber(computerPlayer);

			var humanSettler = (Settlers)Game.Instance.GetUnits().First(unit => unit.Owner == playa.Civilization.Id && unit is Settlers);
			ITile? targetTile = FindFoundCityTargetTile(humanSettler.Tile);
			Assert.NotNull(targetTile);
			int targetX = targetTile.X;
			int targetY = targetTile.Y;

			IUnit? computerSettler = Game.Instance.CreateUnit(UnitType.Settlers, targetX, targetY, computerPlayerId);
			Assert.NotNull(computerSettler);

			Orders.FoundCity(computerSettler).Run();

			City? city = Game.Instance.GetCities().FirstOrDefault(existingCity => existingCity.X == targetX && existingCity.Y == targetY);
			Assert.NotNull(city);
			Assert.Equal(computerPlayerId, city.CityOwnerPlayerIndex);
			Assert.Equal(1, city.Size);
			Assert.Contains(city.ResourceTiles, tile => tile.X == city.X && tile.Y == city.Y);
			Assert.True(city.ResourceTiles.Length >= 2);
			Assert.Empty(city.Specialists);
		}

		[Fact]
		public void UpdateResourcesRelocatesTileOccupiedByEnemyUnit()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 3;

			ITile victim = city.ResourceTiles.First(t => (t.X != city.X || t.Y != city.Y) && !t.IsOcean);

			byte enemyId = Game.Instance.PlayerNumber(
				Game.Instance.Players.First(player => player != null && !player.IsHuman && player.Civilization is not Barbarian));
			Assert.NotNull(Game.Instance.CreateUnit(UnitType.Settlers, victim.X, victim.Y, enemyId));
			Assert.True(city.InvalidTile(victim));

			int workedBefore = city.ResourceTiles.Length;

			city.UpdateResources();

			Assert.DoesNotContain(city.ResourceTiles, t => t.X == victim.X && t.Y == victim.Y);
			Assert.Contains(city.ResourceTiles, t => t.X == city.X && t.Y == city.Y);
			Assert.Equal(workedBefore, city.ResourceTiles.Length);
		}

		[Fact]
		public void UpdateResourcesDoesNotRefillIntentionallyEmptyWorkedTiles()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 3;
			SetResourceTiles(city, []);

			// A city run entirely on specialists (no worked tiles) is a valid player choice.
			// UpdateResources must not silently re-assign workers every turn.
			for (int turn = 0; turn < 3; turn++)
			{
				city.UpdateResources();
			}

			// Only the always-free city-center tile remains.
			Assert.Single(city.ResourceTiles);
			Assert.Equal(city.X, city.ResourceTiles[0].X);
			Assert.Equal(city.Y, city.ResourceTiles[0].Y);
		}

		[Fact]
		public void UpdateResourcesRelocatesMultipleInvalidTilesWithoutError()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.Size = 4;

			byte enemyId = Game.Instance.PlayerNumber(
				Game.Instance.Players.First(player => player != null && !player.IsHuman && player.Civilization is not Barbarian));

			List<ITile> victims =
			[
				.. city.ResourceTiles
					.Where(t => (t.X != city.X || t.Y != city.Y) && !t.IsOcean)
					.Take(2)
			];
			Assert.Equal(2, victims.Count);

			foreach (ITile victim in victims)
			{
				Assert.NotNull(Game.Instance.CreateUnit(UnitType.Settlers, victim.X, victim.Y, enemyId));
			}
			Assert.All(victims, victim => Assert.True(city.InvalidTile(victim)));

			// Iterating the ResourceTiles array snapshot while RelocateResourceTile mutates
			// _resourceTiles must not throw and must clear every invalid tile.
			city.UpdateResources();

			foreach (ITile victim in victims)
			{
				Assert.DoesNotContain(city.ResourceTiles, t => t.X == victim.X && t.Y == victim.Y);
			}
		}

		private static ITile? FindFoundCityTargetTile(ITile origin)
		{
			for (int radius = 0; radius <= 4; radius++)
			{
				for (int relY = -radius; relY <= radius; relY++)
				{
					for (int relX = -radius; relX <= radius; relX++)
					{
						ITile tile = origin[relX, relY];
						if (tile == null || tile.IsOcean || tile.City != null)
						{
							continue;
						}

						return tile;
					}
				}
			}

			return null;
		}

		private static void SetResourceTiles(City city, List<ITile> resourceTiles)
		{
			var field = typeof(City).GetField("_resourceTiles", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(field);

			field.SetValue(city, resourceTiles);
		}

		private static void InvokePrivateSetResourceTiles(City city)
		{
			var method = typeof(City).GetMethod("SetResourceTiles", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(method);

			method.Invoke(city, null);
		}
	}
}