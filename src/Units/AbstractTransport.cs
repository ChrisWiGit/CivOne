// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;

namespace CivOne.Units
{
	internal abstract class AbstractTransport : BaseUnitSea, IBoardable
	{
		public virtual int Cargo { get; }
		public virtual bool AllowedToBoard(IUnit unit)
		{
			if (unit.Class != UnitClass.Land || unit.Owner != Owner)
			{
				return false;
			}

			int availableCargo = Tile.Units.Where(u => u is IBoardable).Sum(u => (u as IBoardable).Cargo);
			int usedCargo = Tile.Units.Count(u => u.Class == UnitClass.Land);

			return availableCargo > usedCargo;
		}

		protected AbstractTransport(byte price = 1, byte attack = 1, byte defense = 1, byte move = 1, int range = 1) : base(price, attack, defense, move, range)
		{
			Type = UnitType.Transport;
		}
    }
}