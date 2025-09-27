// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units.TribalHuts;
using CivOne.UserInterface;

namespace CivOne.Units
{
	internal abstract class BaseUnitLand : BaseUnit
	{
		private bool FastRiverMovement => Settings.Instance.RiverFastMovement;
		private byte FastRiverMovementCount => FastRiverMovement ? (byte)3 : (byte)1;

		protected override void MovementDone(ITile previousTile)
		{
			if (previousTile.IsOcean || Tile.IsOcean)
			{
				SkipTurn();
			}

			Tile.Visit(Owner);
			VisitHut();

			if (
				(previousTile.Road || previousTile.RailRoad) && (Tile.Road || Tile.RailRoad) ||
				(FastRiverMovement && previousTile is River && Tile is River))
			{
				TravelOnRoad(previousTile);

				return;
			}

			if (Tile.Type == Terrain.Ocean)
			{
				BoardShip(previousTile);

				return;
			}

			MoveOnYourOwn();
		}

		private void VisitHut()
		{
			if (!Tile.Hut)
			{
				return;
			}

			Tile.Hut = false;

			TribalHutsVisitorProvider.provide(
				Player,
				Map,
				currentUnit: this,
				Game.Instance,
				Game.Instance,
				Common.Random
			).ExecuteRandomTribalHutEvent();
		}

		private void BoardShip(ITile previousTile)
		{
			bool unloadFromShip = !previousTile.IsOcean;

			if (unloadFromShip)
			{
				SkipTurn();
			}

			Sentry = true;
			foreach (IUnit unit in Tile.Units.Where(u => u is IBoardable))
			{
				unit.Sentry = false;
			}
		}

		private void MoveOnYourOwn()
		{
			Debug.Assert(Class == UnitClass.Land);

			PartMoves = 0; // fire-eggs 20190806 we've moved off-road: all partial moves always lost
			if (MovesLeft == 0)
			{
				return;
			}

			byte moveCosts = Tile.Movement;

			if (MovesLeft < moveCosts)
				moveCosts = MovesLeft;
			MovesLeft -= moveCosts;
		}

		private void TravelOnRoad(ITile previousTile)
		{
			bool continuousTravelingOnRailRoad = (Tile.RailRoad || Tile.City != null) && previousTile.RailRoad;

			if (continuousTravelingOnRailRoad)
			{
				// No moves lost
				return;
			}

			if (PartMoves > 0)
			{
				PartMoves--;
			}
			else
			{
				if (MovesLeft > 0)
					MovesLeft--;
				PartMoves = (byte)(Tile is River ? FastRiverMovementCount : 1);
			}
		}

		public override IEnumerable<MenuItem<int>> MenuItems
		{
			get
			{
				yield return MenuNoOrders();
				yield return MenuFortify();
				yield return MenuWait();
				yield return MenuSentry();
				yield return MenuGoTo();
				if (Map[X, Y].Irrigation || Map[X, Y].Mine || Map[X, Y].Road || Map[X, Y].RailRoad)
				{
					yield return MenuPillage();
				}
				if (Map[X, Y].City != null)
				{
					yield return MenuHomeCity();
				}
				yield return null;
				yield return MenuDisbandUnit();
			}
		}

		public override bool MoveTo(int relX, int relY)
		{
			// no base.MoveTo. We handle everything on our own.
			Debug.Assert(Class == UnitClass.Land);

			if (Movement != null) return false;

			ITile moveTarget = Map[X, Y][relX, relY];
			if (moveTarget == null) return false;


			bool? attacked = ConfrontCity(moveTarget, relX, relY);
			if (attacked.HasValue)
			{
				return attacked.Value;
			}

			// if (!MoveTargets.Any(t => t.X == moveTarget.X && t.Y == moveTarget.Y))
			if (!MoveTargets.Any(t => t.SameLocationAs(moveTarget)))
			{
				// Target tile is invalid
				// TODO: For some tiles, display a message detailing why the move is illegal
				return false;
			}

			// Checking Enemy Confrontation after MoveTargets check,
			// a land unit cannot attack an enemy city on ocean tile
			if (HasEnemyOnTarget(moveTarget))
			{
				return ConfrontEnemy(moveTarget, relX, relY);
			}

			// if (Class == UnitClass.Land && !(this is Diplomat || this is Caravan))
			if (this is not (Diplomat or Caravan))
			{
				if (!CanMoveTo(relX, relY))
				{
					if (Human == Owner)
					{
						Goto = Point.Empty;             // Cancel any goto mode ( maybe for AI too ?? )
						GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText($"ERROR/ZOC")));
					}
					return false;
				}
			}

			if (!IsFastRoadTraveling(relX, relY, moveTarget))
			{
				if (!TryLeavingRoad(relX, relY, moveTarget))
				{
					return false;
				}

				if (HasHitObstacle(relX, relY, moveTarget))
				{
					return false;
				}
			}

			MovementTo(relX, relY);
			return true;
		}

		// auf road oder river, wie in newmethod
		private bool IsFastRoadTraveling(int relX, int relY, ITile moveTarget)
		{
			bool isOnRoad = (moveTarget.Road || moveTarget.RailRoad) && (Tile.Road || Tile.RailRoad);
			bool isOnRiver = FastRiverMovement && moveTarget is River && Tile is River;

			return isOnRoad || isOnRiver;
		}

		private bool TryLeavingRoad(int relX, int relY, ITile moveTarget)
		{
			bool exitRoad = !moveTarget.Road && moveTarget.Movement > 1;

			if (MovesLeft > 0 || !exitRoad)
			{
				return true;
			}

			if (!AllowedToLeaveRoad(moveTarget, relX, relY))
			{
				return false;
			}

			return true;
		}


		private bool HasHitObstacle(int relX, int relY, ITile moveTarget)
		{
			bool isHumanMove = Human == Owner;
			bool isMultiMoveUnit = Move > 1; // CW: only by observation
			bool hasNotEnoughMoves = MovesLeft < moveTarget.Movement;

			if (!isHumanMove || !isMultiMoveUnit || !hasNotEnoughMoves)
			{
				return false;
			}
			
			// 10% chance of failure if not enough moves left to enter the tile
			var failure = Common.Random.Hit(10);
			if (failure)
			{
				SkipTurn();
				return true;
			}

			return false;
		}

		protected virtual bool AllowedToLeaveRoad(ITile moveTarget, int relX, int relY)
		{
			bool success;
			if (PartMoves >= 2)
			{
				// 2/3 moves left? 50% chance of success
				success = Common.Random.Next(0, 2) == 0;
			}
			else
			{
				// 1/3 moves left? 33% chance of success
				success = Common.Random.Next(0, 3) == 0;
			}

			if (success)
			{
				return true;
			}

			PartMoves = 0;
			return false;
		}


		protected override bool ConfrontEnemy(ITile moveTarget, int relX, int relY)
		{
			if (Tile.IsOcean)
			{
				if (Human == Owner)
					GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText($"ERROR/AMPHIB")));
				return false;
			}

			return base.ConfrontEnemy(moveTarget, relX, relY);
		}



		protected override bool ValidMoveTarget(ITile tile)
		{
			if (tile == null)
				return false;

			// If the tile is not an ocean tile, movement is allowed
			if (tile.Type != Terrain.Ocean)
				return true;

			bool isOwner = tile.Units.Any(u => u.Owner == Owner);
			bool allowedToBoard = tile.Units.Where(u => u is IBoardable).Any(u => (u as IBoardable).AllowedToBoard(this));
			bool hasFreeSlots = tile.Units.Where(u => u is IBoardable).Sum(u => (u as IBoardable).Cargo) > tile.Units.Count(u => u.Class == UnitClass.Land);

			return isOwner && allowedToBoard && hasFreeSlots;
		}

		protected BaseUnitLand(byte price = 1, byte attack = 1, byte defense = 1, byte move = 1) : base(price, attack, defense, move)
		{
			Class = UnitClass.Land;
			Role = UnitRole.LandAttack;
		}
	}
}