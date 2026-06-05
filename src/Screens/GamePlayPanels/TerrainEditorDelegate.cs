// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens.GamePlayPanels
{
	internal sealed class TerrainEditorDelegate
	{
		private readonly int[] _brushSizes = [1, 2, 3, 5, 7, 9, 11, 13, 15];

		internal int BrushSizeCount => _brushSizes.Length;

		internal int GetBrushSize(int brushIndex)
		{
			int normalizedIndex = Math.Clamp(brushIndex, 0, _brushSizes.Length - 1);
			return _brushSizes[normalizedIndex];
		}

		private void GetRelativeBounds(int brushIndex, out int minRel, out int maxRel)
		{
			int size = GetBrushSize(brushIndex);
			minRel = -((size - 1) / 2);
			maxRel = size / 2;
		}

		private void ApplyToBrush(int centerX, int centerY, int brushIndex, Action<int, int> action)
		{
			GetRelativeBounds(brushIndex, out int minRel, out int maxRel);
			for (int relY = minRel; relY <= maxRel; relY++)
			{
				int targetY = Map.EditorClampY(centerY + relY);
				for (int relX = minRel; relX <= maxRel; relX++)
				{
					int targetX = Map.EditorWrapX(centerX + relX);
					action(targetX, targetY);
				}
			}
		}

		internal void ApplyBrush(int centerX, int centerY, int brushIndex, Terrain terrain)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) => Map.Instance.EditorSetTerrain(x, y, terrain));
		}

		internal void SetIrrigation(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Irrigation = true;
				tile.Mine = false;
			});
		}

		internal void RemoveIrrigation(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Irrigation = false;
			});
		}

		internal void AddRoad(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				if (tile.Road)
				{
					tile.RailRoad = true;
				}
				else
				{
					tile.Road = true;
				}
			});
		}

		internal void RemoveRoad(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				if (tile.RailRoad)
				{
					tile.RailRoad = false;
				}
				else
				{
					tile.Road = false;
				}
			});
		}

		internal void SetMine(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Mine = true;
				tile.Irrigation = false;
			});
		}

		internal void RemoveMine(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Mine = false;
			});
		}

		internal void SetFortress(int centerX, int centerY, int brushIndex, bool enabled)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Fortress = enabled;
			});
		}

		internal void SetPollution(int centerX, int centerY, int brushIndex, bool enabled)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Pollution = enabled;
			});
		}

		internal void SetHut(int centerX, int centerY, int brushIndex, bool enabled)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Hut = enabled;
			});
		}

		internal void ClearImprovements(int centerX, int centerY, int brushIndex)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				tile.Road = false;
				tile.RailRoad = false;
				tile.Irrigation = false;
				tile.Mine = false;
				tile.Fortress = false;
				tile.Pollution = false;
				tile.Hut = false;
			});
		}

		internal void AdjustLandValue(int centerX, int centerY, int brushIndex, int delta)
		{
			ApplyToBrush(centerX, centerY, brushIndex, (x, y) =>
			{
				ITile tile = Map.Instance[x, y];
				if (tile == null)
				{
					return;
				}

				int newValue = Math.Clamp(tile.LandValue + delta, 0, 255);
				tile.LandValue = (byte)newValue;
			});
		}

		internal static void EditCitySingleTile(int x, int y, byte cityOwner, bool shrink)
		{
			Game game = Game.Instance;
			ITile tile = Map.Instance[x, y];
			if (tile == null)
			{
				return;
			}

			City? city = game.GetCity(x, y);

			if (city == null)
			{
				HandleCityFounding(x, y, cityOwner, shrink, game, tile);
				return;
			}

			if (city.Owner != cityOwner)
			{
				return;
			}

			if (shrink)
			{
				if (city.Size > 1)
				{
					city.Size--;
				}
				else
				{
					city.Size = 0;
				}
			}
			else
			{
				if (city.Size < byte.MaxValue)
				{
					city.Size++;
				}
			}
		}

		private static bool HandleCityFounding(int x, int y, byte cityOwner, bool shrink, Game game, ITile tile)
		{
			if (shrink)
			{
				return false;
			}

			if (tile.IsOcean)
			{
				return false;
			}

			Player? owner = game.GetPlayer(cityOwner);
			if (owner == null)
			{
				return false;
			}

			int nameId = game.CityNameId(owner);
			game.AddCity(owner, nameId, x, y);
			return true;
		}

		private static bool CanPlaceUnit(ITile tile, IUnit selectedUnit, byte ownerId)
		{
			if (tile.Units.Any(x => x.Owner != ownerId))
			{
				return false;
			}

			if (selectedUnit.Class == UnitClass.Land && tile.City != null)
			{
				return ownerId == tile.City.Owner;
			}

			if (selectedUnit.Class == UnitClass.Land && tile.Type == Terrain.Ocean)
			{
				if (!tile.Units.Any(x => x.Class == UnitClass.Water && x is IBoardable))
				{
					return false;
				}

				int capacity = tile.Units.Where(x => x.Class == UnitClass.Water).OfType<IBoardable>()
									.Sum(x => x.Cargo);
				int unitCount = tile.Units.Count(x => x.Class == UnitClass.Land);
				return unitCount < capacity;
			}

			if (selectedUnit.Class == UnitClass.Water && tile.Type != Terrain.Ocean)
			{
				return tile.City != null && ownerId == tile.City.Owner;
			}

			return true;
		}

		internal static bool SpawnUnit(int x, int y, byte unitOwner, UnitType unitType)
		{
			Game game = Game.Instance;
			ITile tile = Map.Instance[x, y];
			if (tile == null)
			{
				return false;
			}

			if (game.GetPlayer(unitOwner) == null)
			{
				return false;
			}

			IUnit? selectedUnit = Game.CreateUnit(unitType);
			if (selectedUnit == null || !CanPlaceUnit(tile, selectedUnit, unitOwner))
			{
				return false;
			}

			IUnit? unit = game.CreateUnit(unitType, x, y, unitOwner, false);
			if (unit == null)
			{
				return false;
			}

			if (unit.Class == UnitClass.Land && tile.Type == Terrain.Ocean)
			{
				unit.Sentry = true;
			}

			if (unitOwner < game.PlayerNumber(game.CurrentPlayer))
			{
				unit.MovesLeft = 0;
			}

			if (unit.Class == UnitClass.Land && tile.Hut)
			{
				tile.Hut = false;
			}

			if (unit is BaseUnitAir airUnit)
			{
				airUnit.FuelLeft = airUnit.TotalFuel;
			}

			unit.Explore();
			return true;
		}

		#pragma warning disable CA1822
		internal bool RemoveUnit(int x, int y, byte unitOwner, UnitType unitType)
		{
			ITile tile = Map.Instance[x, y];
			if (tile == null)
			{
				return false;
			}

			IUnit? unit = tile.Units.FirstOrDefault(u => u.Owner == unitOwner && u.Type == unitType)
				?? tile.Units.FirstOrDefault(u => u.Owner == unitOwner);
			if (unit == null)
			{
				return false;
			}

			Game.Instance.DisbandUnit(unit);
			return true;
		}
		#pragma warning restore CA1822
	}
}