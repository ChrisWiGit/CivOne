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
		public int TotalFuel { get; protected set; }
		public int FuelLeft { get; set; }


	// Check for room on flight deck
	private bool CarrierIsLandable()
	{
		IUnit[] CUnits;
		IUnit[] AUnits;
		CUnits = Map [X, Y].Units.Where(u => u.Name == "Carrier" ).ToArray();
		if (CUnits.Length == 0)
			return false;
		else
		{
			// Check load on flight deck
			AUnits = Map[X, Y].Units.Where(u => u.Class == UnitClass.Air).ToArray();
			if( AUnits.Length > CUnits.Length * 8 )
				return false;
		}
		return true;
	}


	private void HandleFuel()
		{
			if ( Map[X, Y].Units.Any(u => u.Name == "Carrier" ))
			{
				if (CarrierIsLandable())
				{
					MovesLeft = 0;
					FuelLeft = TotalFuel;		// Refuel
					return;
				}
			}

			if (Map[X, Y].City != null )
			{
				MovesLeft = 0;
				FuelLeft = TotalFuel;			// Refuel
				return;
			}


			if (MovesLeft > 0 || FuelLeft > 0) return;
			
			// Air unit is out of fuel
			Game.DisbandUnit(this);
			GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText("ERROR/FUEL")));
		}

		protected override void MovementDone(ITile previousTile)
		{
			base.MovementDone(previousTile);
			
			FuelLeft--;
			HandleFuel();
		}

		public override void SkipTurn()
		{
			MovesLeft = 0;
			if (FuelLeft == Move)
				FuelLeft -= Move;
			FuelLeft -= (FuelLeft % Move);
			HandleFuel();
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
			return (tile != null);
		}



	protected BaseUnitAir(byte price = 1, byte attack = 1, byte defense = 1, byte move = 1) : base(price, attack, defense, move)
		{
			Class = UnitClass.Air;
			TotalFuel = move;
			FuelLeft = move;
		}
	}
}