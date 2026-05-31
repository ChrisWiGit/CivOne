using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Tasks;
using CivOne.Tiles;
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
			City city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

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
			City city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

			city.Size = 4;

			Assert.Equal(city.Size + 1, city.ResourceTiles.Length);
		}

		[Fact]
		public void SizeIncreaseStopsWhenNoMoreTilesAvailable()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

			city.Size = 30;

			Assert.True(city.ResourceTiles.Length <= 21);
		}

		[Fact]
		public void SetResourceTilesBreaksWhenNoBestTileExists()
		{
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

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
			Player barbarian = Game.Instance.GetPlayer(0);
			City city = Game.Instance.AddCity(barbarian, 1, unit.X, unit.Y);

			Assert.Contains(city.ResourceTiles, tile => tile.X == city.X && tile.Y == city.Y);
			Assert.True(city.ResourceTiles.Length >= 2);
			Assert.Empty(city.Specialists);
		}

		[Fact]
		public void FoundCityOrderForComputerOwnerKeepsWorkedTilesAndNoSpecialistsAtSizeOne()
		{
			Player computerPlayer = Game.Instance.Players.First(player => !player.IsHuman);
			byte computerPlayerId = Game.Instance.PlayerNumber(computerPlayer);

			var humanSettler = (Settlers)Game.Instance.GetUnits().First(unit => unit.Owner == playa.Civilization.Id && unit is Settlers);
			int targetX = humanSettler.X;
			int targetY = humanSettler.Y;

			IUnit? computerSettler = Game.Instance.CreateUnit(UnitType.Settlers, targetX, targetY, computerPlayerId);
			Assert.NotNull(computerSettler);

			GameTask.Enqueue(Orders.FoundCity(computerSettler));
			RunAllQueuedTasks();

			City? city = Game.Instance.GetCities().FirstOrDefault(existingCity => existingCity.X == targetX && existingCity.Y == targetY);
			Assert.NotNull(city);
			Assert.Equal(computerPlayerId, city.Owner);
			Assert.Equal(1, city.Size);
			Assert.Contains(city.ResourceTiles, tile => tile.X == city.X && tile.Y == city.Y);
			Assert.True(city.ResourceTiles.Length >= 2);
			Assert.Empty(city.Specialists);
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

		private static void RunAllQueuedTasks()
		{
			const int maxTaskUpdates = 20;
			int updates = 0;

			while (GameTask.Update())
			{
				updates++;
				Assert.True(updates < maxTaskUpdates);
			}
		}
	}
}