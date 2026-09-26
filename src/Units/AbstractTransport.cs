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
			if (unit.UnitCategory != UnitClass.Land || unit.Owner != Owner)
			{
				return false;
			}

			int availableCargo = Tile.Units.OfType<IBoardable>().Sum(u => u.Cargo);
			int usedCargo = Tile.Units.Count(u => u.UnitCategory == UnitClass.Land);

			return availableCargo > usedCargo;
		}

		protected AbstractTransport(byte price = 1, byte attack = 1, byte defense = 1, byte move = 1, int range = 1) : base(price, attack, defense, move, range)
		{
			Type = UnitType.Transport;
		}
    }
}