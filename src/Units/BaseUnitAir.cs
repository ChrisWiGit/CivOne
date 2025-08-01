// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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


		// Check for room on flight deck
		protected bool CanLandOnCarrier()
		{
			return (Tile.Units.FirstOrDefault(u => u is Carrier) as IBoardable)?.AllowedToBoard(this) ?? false;
		}


		private void HandleFuel(byte movesLeft = 0)
		{
			if (Map[X, Y].Units.Any(u => u is Carrier))
			{
				if (CanLandOnCarrier())
				{
					MovesLeft = movesLeft;
					FuelLeft = TotalFuel;
					return;
				}
			}

			if (Map[X, Y].City != null)
			{
				MovesLeft = movesLeft;
				FuelLeft = TotalFuel;
				return;
			}

			// TODO: CW: We should disallow setting sentry (or just remove it here?) -> This would be a convenience setting for the player.
			if (!Sentry && (movesLeft > 0 || FuelLeft > 0)) return;

			// Air unit is out of fuel
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
			HandleFuel(TotalFuel);
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