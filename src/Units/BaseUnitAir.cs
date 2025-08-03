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
using System.Linq;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Screens;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.UserInterface;



namespace CivOne.Units
{
	internal abstract class BaseUnitAir : BaseUnit
	{
		public byte TotalFuel { get; protected set; }

		// Store it in FuelOrProgress so that it can be saved and loaded from save files
		public byte FuelLeft
		{
			get => FuelOrProgress;
			set => FuelOrProgress = value;
		}


		protected bool CanLandOnCarrier()
		{
			return (Tile.Units.FirstOrDefault(u => u is Carrier) as IBoardable)?.AllowedToBoard(this) ?? false;
		}


		protected bool TryToLandOnCarrier(byte movesLeft = 0)
		{
			if (!AbleToLandOnCarrier(0))
			{
				HandleFuel();
				return false;
			}
			MovesLeft = movesLeft;
			FuelLeft = TotalFuel;

			return true;
		}
		protected void HandleFuel(byte movesLeft = 0)
		{
			if (AbleToLandOnCarrier(movesLeft))
			{
				return;
			}
			if (TryRefuelAtCity(movesLeft)) return;

			// TODO: CW: We should disallow setting sentry (or just remove it here?) -> This would be a convenience setting for the player.
			if (!Sentry && (movesLeft > 0 || FuelLeft > 0)) return;

			HandleOutOfFuel();
		}

		private bool AbleToLandOnCarrier(byte movesLeft)
		{
			if (!Map[X, Y].Units.Any(u => u is Carrier))
			{
				return false;
			}
			if (!CanLandOnCarrier())
			{
				return false;
			}

			return true;
		}

		private bool TryRefuelAtCity(byte movesLeft)
		{
			if (Map[X, Y].City == null)
			{
				return false;
			}

			MovesLeft = movesLeft;
			FuelLeft = TotalFuel;

			return true;
		}

		private void HandleOutOfFuel()
		{
			// TODO: CW: We should disallow setting sentry (or just remove it here?) -> This would be a convenience setting for the player.
			Game.DisbandUnit(this);
			GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText("ERROR/FUEL")));
		}

		protected override void MovementDone(ITile previousTile)
		{
			if (Tile.Hut)
			{
				// Destroy hut without any action
				Tile.Hut = false;
			}

			base.MovementDone(previousTile);

			FuelLeft--;
			HandleFuel();
		}

		public override void SkipTurn()
		{
			MovesLeft = 0;
			HandleFuel();
		}

		public override void SentryOnShip()
		{
			byte movesLeft = MovesLeft;
			// air units must land
			SentryWithoutSkipTurn();
			HandleFuel(movesLeft);
		}

		public override void NewTurn()
		{
			base.NewTurn();
			TryToLandOnCarrier();
		}

		public override IEnumerable<MenuItem<int>> MenuItems
		{
			get
			{
				ITile tile = Map[X, Y];

				yield return MenuNoOrders();
				yield return MenuWait();
				yield return MenuSentry();
				yield return MenuGoTo();
				if ((TotalFuel > Move) && (tile.Irrigation || tile.Mine || tile.Road || tile.RailRoad))
				{
					yield return MenuPillage();
				}
				if (tile.City != null)
				{
					yield return MenuHomeCity();
				}
				yield return null;
				yield return MenuDisbandUnit();
			}
		}

		protected override bool ValidMoveTarget(ITile tile)
		{
			return tile != null;
		}



		protected BaseUnitAir(byte price = 1, byte attack = 1, byte defense = 1, byte movesCount = 1) : base(price, attack, defense, movesCount)
		{
			Class = UnitClass.Air;
			TotalFuel = movesCount;
			FuelLeft = movesCount;
		}
	}
}