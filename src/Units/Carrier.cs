// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Units
{
	internal class Carrier : AbstractTransport
	{
		public static readonly int MAX_CARGO = 8;
		public override int Cargo => MAX_CARGO;

		protected override IEnumerable<IUnit> MoveUnits(ITile previousTile)
		{
			if (this is not IBoardable || !previousTile.Units.Any(u => u.Class == UnitClass.Air))
			{
				yield break;
			}

			IUnit[] moveUnits = [.. previousTile.Units.Where(u => u.Class == UnitClass.Air)];

			if (previousTile.City != null)
			{
				moveUnits = [.. moveUnits.Where(u => u.Sentry)];
			}

			foreach (IUnit unit in moveUnits.Take(Cargo))
			{
				yield return unit;
			}
		}

		public override bool AllowedToBoard(IUnit unit)
		{
			if (unit.Class != UnitClass.Air || unit.Owner != Owner)
			{
				return false;
			}

			int availableCargo = Tile.Units.Where(u => u is IBoardable).Sum(u => (u as IBoardable).Cargo);
			int usedCargo = Tile.Units.Count(u => u.Class == UnitClass.Air);

			return availableCargo >= usedCargo;
		}

		public Carrier() : base(16, 1, 12, 5, 2)
		{
			Type = UnitType.Carrier;
			Name = "Carrier";
			RequiredTech = new AdvancedFlight();
			ObsoleteTech = null;
			SetIcon('D', 1, 0);
			Role = UnitRole.Transport;
		}
	}
}