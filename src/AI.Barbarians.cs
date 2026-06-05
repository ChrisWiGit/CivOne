// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Services.Pathfinding;
using CivOne.Services.Random;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Drawing;
using System.Linq;

namespace CivOne
{
	/// <summary>
	/// AI logic for barbarian units. 
	/// This version was updated from 
	/// <a href="https://github.com/mwerneburg/CivOne/commit/eec2410b583cd3c119cd3889fecc579bcffa4374">mwerneburg/CivOne</a> to use the new A* pathfinding implementation 
	/// in <see cref="UnitGotoServiceImpl2"/>, while preserving the same behaviour and logic for barbarian movement. 
	/// </summary>
    internal partial class AI
	{
		private readonly IUnitGotoService _unitGotoService = UnitGotoServiceFactory.Create();
		private readonly IRandomService _randomService = RandomServiceFactory.Create();

		private static bool IsPolarTile(ITile tile) => tile.Type == Terrain.Arctic;

		private void BarbarianMove(IUnit unit)
		{
			switch (unit.Class)
			{
				case UnitClass.Water:
					BarbarianMoveWater(unit);
					return;
				case UnitClass.Land:
					BarbarianMoveLand(unit);
					return;
				default:
					Game.DisbandUnit(unit);
					return;
			}
		}

		private void BarbarianMoveWater(IUnit unit)
		{
			if (!unit.Tile.Units.Any(x => x.Class == UnitClass.Land))
			{
				Game.DisbandUnit(unit);
				return;
			}

			for (int i = 0; i < 1000; i++)
			{
				// Only try to disembark when there are enemy-free land tiles to land on.
				// If every adjacent land tile is occupied by a non-barbarian, fall through
				// to the Goto navigation so the ship can seek a better landing spot.
				ITile[] landingZones = [.. unit.Tile.GetBorderTiles().Where(x => !x.IsOcean && !IsPolarTile(x) && !x.Units.Any(u => u.Owner != 0))];
				if (landingZones.Length > 0)
				{
					if (Game.GetCities().Any(x => x.Owner != 0))
					{
						City nearestCity = Game.GetCities().Where(x => x.Owner != 0).OrderBy(x => Common.DistanceToTile(x.X, x.Y, unit.X, unit.Y)).ThenBy(x => x.Player == Human ? 0 : 1).First();
						if (nearestCity.Player == Human && Human.Visible(unit.Tile))
						{
							GameTask.Insert(Message.Advisor(Advisor.Defense, false,
								TranslateFormattedArray("Barbarian raiding party\nlands near {0}!\nCitizens are alarmed.", nearestCity.Name)));
						}
					}

					// Aboard units are invisible to ActiveUnit so UnitWait can never unblock.
					// Move each land unit directly to a landing tile instead.
					foreach (IUnit landUnit in unit.Tile.Units.Where(x => x.Class == UnitClass.Land).ToList())
					{
						landUnit.Sentry = false;
						ITile dest = landingZones[_randomService.NextInt(landingZones.Length)];
						landUnit.MoveTo(dest.X - landUnit.X, dest.Y - landUnit.Y);
					}
					unit.SkipTurn();
					return;
				}

				if (unit.Goto.IsEmpty)
				{
					// Target a coastal ocean tile adjacent to the nearest palace city.
					// Targeting the city tile itself would make GotoStep fail (water→land),
					// causing an infinite re-targeting loop.
					City? nearestCity = Game.GetCities()
						.Where(x => x.Owner != 0 && x.HasBuilding<Palace>()
								&& x.Tile.GetBorderTiles().Any(t => t.IsOcean && !IsPolarTile(t)))
						.OrderBy(x => Common.DistanceToTile(x.X, x.Y, unit.X, unit.Y))
						.FirstOrDefault();

					if (nearestCity == null
						|| Common.DistanceToTile(unit.X, unit.Y, nearestCity.X, nearestCity.Y) > 10)
					{
						Game.DisbandUnit(unit);
						return;
					}

					ITile approach = nearestCity.Tile.GetBorderTiles()
						.Where(t => t.IsOcean && !IsPolarTile(t))
						.OrderBy(t => Common.DistanceToTile(unit.X, unit.Y, t.X, t.Y))
						.First();

					unit.Goto = new Point(approach.X, approach.Y);
					continue;
				}

				if (!unit.Goto.IsEmpty)
				{
					ITile next = _unitGotoService.GotoStep(unit);
					if (next == null)
					{
						// No path to current target — give up for this turn.
						unit.Goto = Point.Empty;
						unit.SkipTurn();
						return;
					}
					if (!unit.MoveTo(next.X - unit.X, next.Y - unit.Y))
					{
						unit.Goto = Point.Empty;
						unit.SkipTurn();
					}
					return;
				}

				unit.SkipTurn();
				return;
			}

			// Safety fallback: loop exhausted without resolving — skip turn.
			unit.SkipTurn();
		}

		private void BarbarianMoveLand(IUnit unit)
		{
			if (unit.Tile.IsOcean && unit.Tile.GetBorderTiles().Where(x => !x.IsOcean && !IsPolarTile(x)).All(x => x.Units.Any(u => u.Owner != 0)))
			{
				IUnit? ship = unit.Tile.Units.FirstOrDefault(u => u.Class == UnitClass.Water && u.MovesLeft > 0);
				if (ship != null)
				{
					ITile[] landTiles = [.. unit.Tile.GetBorderTiles().Where(x => !x.IsOcean && !IsPolarTile(x) && x.Units.Any(u => u.Owner != 0))];
					if (landTiles.Length > 0)
					{
						ITile tile = landTiles[_randomService.NextInt(landTiles.Length)];
						if (!ship.MoveTo(tile.X - unit.X, tile.Y - unit.Y))
							unit.SkipTurn();
						return;
					}
				}
				unit.SkipTurn();
				return;
			}

			if (unit is Diplomat)
			{
				if (unit.WorkProgress <= 0)
				{
					// A barbarian leader was able to flee after at max of 30 turns of pursuit.
					Game.DisbandUnit(unit);
					return;
				}

				unit.WorkProgress = unit.WorkProgress == 0 ? (byte)0 : (byte)(unit.WorkProgress - 1);

				ITile[] friendlyTiles = [.. unit.Tile.GetBorderTiles().Where(x => !x.IsOcean && !IsPolarTile(x) && x.Units.Length != 0 && x.Units[0].Owner == 0)];
				if (friendlyTiles.Length > 0)
				{
					ITile moveTo = friendlyTiles[_randomService.NextInt(friendlyTiles.Length)];
					int relX = moveTo.X - unit.X;
					int relY = moveTo.Y - unit.Y;
					unit.MoveTo(relX, relY);
					unit.WorkProgress = (byte)(10 + _randomService.NextByte(0, 20));
					return;
				}

				if (unit.Tile.Units.Any(x => x is not Diplomat && x.MovesLeft > 0))
				{
					unit.SkipTurn();
					return;
				}

				if (unit.Tile.Units.Any(x => x is not Diplomat))
				{
					unit.SkipTurn();
					return;
				}

				ITile[] unfriend = [.. unit.Tile.GetBorderTiles().Where(z => !z.IsOcean && !IsPolarTile(z) && z.Units.Length == 0)];
				if (unfriend.Length > 0)
				{
					ITile moveTo = unfriend[_randomService.NextInt(unfriend.Length)];
					int relX = moveTo.X - unit.X;
					int relY = moveTo.Y - unit.Y;
					unit.MoveTo(relX, relY);
					return;
				}

				unit.SkipTurn();
				return;
			}

			ITile[] tiles = unit.Tile.GetBorderTiles().Where(t => !((unit.Tile.IsOcean || unit is Diplomat) && t.City != null) && !t.IsOcean && !IsPolarTile(t) && t.Units.Any(u => u.Owner != 0)).ToArray();
			if (tiles.Length == 0)
			{
				// No adjacent units found
				bool moved = MoveAwayOrDisband(unit);
				if (!moved)
				{
					return;
				}
			}
			else
			{
				ITile moveTo = tiles[_randomService.NextInt(tiles.Length)];
				int relX = moveTo.X - unit.X;
				int relY = moveTo.Y - unit.Y;
				while (relX < -1) relX += Map.WIDTH;
				while (relX > 1) relX -= Map.WIDTH;
				if (unit is Diplomat && unit.Tile.City != null) return;
				if (unit.Attack == 0) 
				{
					// Units that cannot attack will try to move away from enemies instead of towards them.
					MoveAwayOrDisband(unit);
					return;
				}

				unit.MoveTo(relX, relY);
			}
		}

		private static bool MoveAwayOrDisband(IUnit unit)
		{
			IRandomService randomService = RandomServiceFactory.Create();

			if (randomService.NextInt(100) < 95)
			{
				for (int i = 0; i < 1000; i++)
				{
					int relX = randomService.NextInt(-1, 2);
					int relY = randomService.NextInt(-1, 2);
					if (relX == 0 && relY == 0) continue;
					if (unit.Tile[relX, relY] is Ocean || IsPolarTile(unit.Tile[relX, relY])) continue;
					if (unit is Diplomat && unit.Tile[relX, relY].City != null) continue;
					if (unit.Tile.IsOcean && unit.Tile[relX, relY].City != null) continue;
					if (unit.Attack == 0) continue; // Units that cannot attack will try to move away from enemies instead of towards them.
					unit.MoveTo(relX, relY);
					return false;
				}
			}
			Game.DisbandUnit(unit);
			return true;
		}
	}
}