// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tasks;
using CivOne.Tiles;
using System.Linq;
using CivOne.Buildings;
using System.Collections.Generic;
using CivOne.src;

namespace CivOne.Units
{
	internal class Diplomat : BaseUnitLand
	{
		public static bool CanIncite(City cityToIncite, short gold)
		{
			return gold >= InciteCost(cityToIncite) && !cityToIncite.HasBuilding<Palace>();
		}

		public static int InciteCost(City cityToIncite)
		{
			City capital = cityToIncite.Player.GetCapital();

			int distance = capital == null ? 16 : cityToIncite.Tile.DistanceTo(capital);
			
			int cost = (cityToIncite.Player.Gold + 1000) / (distance + 3);

			// if city is in disorder need to halve the cost
            if (cityToIncite.IsInDisorder)
                cost /= 2;

			return cost;
		}

		public IAdvance GetAdvanceToSteal(Player victim)
		{
			IList<IAdvance> possible = victim.Advances.Where(p => !Player.Advances.Any(p2 => p2.Id == p.Id)).ToList();

			if (!possible.Any())
				return null;

			return possible[Common.Random.Next(0, possible.Count - 1)];
		}

		public string Sabotage(City city)
		{
			Game.DisbandUnit(this);

			IList<IBuilding> buildings = city.Buildings.Where(b => (b.GetType() != typeof(Buildings.Palace))).ToList();

			int random = Common.Random.Next(0, buildings.Count);

			if (random == buildings.Count)
			{
				city.Shields = (ushort)0;
				string production = (city.CurrentProduction as ICivilopedia).Name;
				return $"{production} production sabotaged";
			}
			else
			{
				// sabotage a building
				city.RemoveBuilding(buildings[random]);
				return $"{buildings[random].Name} sabotaged";
			}
		}

		internal override bool Confront(int relX, int relY)
		{
			ITile moveTarget = Map[X, Y][relX, relY];

            var targetCity = moveTarget.City;
			if (targetCity != null)
			{
				if (Human == Owner)
				{
					GameTask.Enqueue(Show.DiplomatCity(targetCity, this));
				}
                else if (Human == targetCity.Player)
                {
                    GameTask.Enqueue(Message.Spy("Spies report:", $"{Sabotage(targetCity)}", $"in {targetCity.Name}"));
                }
				else
					Sabotage(targetCity);
					
				return true;
			}

			IUnit[] units = moveTarget.Units;

			if (units.Length == 1)
			{
				IUnit unit = units[0];

                // TODO fire-eggs bribe ocean unit
				if (Human == Owner && unit.Owner != Owner && unit is BaseUnitLand)
				{
					GameTask.Enqueue(Show.DiplomatBribe(unit as BaseUnitLand, this));
					return true;
				}
			}

			MovementTo(relX, relY);
			return true;
		}

		internal void KeepMoving(IUnit unit) => MovementTo(unit.X - X, unit.Y - Y);

		public Diplomat() : base(3, 0, 0, 2)
		{
			Type = UnitType.Diplomat;
			Name = "Diplomat";
			RequiredTech = new Writing();
			ObsoleteTech = null;
			SetIcon('C', 1, 0);
			Role = UnitRole.Civilian;
			// CW: Hot fix to disband AI Diplomat units, when WorkProgress hits 0
			// WorkProgress will be reset if Diplomat finds "friendly" unit.
			// 10 = 5 turns of 2 moving points + random max 10 more turns [from 5 up to 15 turns]
			// Resets if friendly unit found.
			// Duplicate code in AI.Barbarians.cs
			// WARNING: This is not from the original code!
			WorkProgress = (byte)(10 + Common.Random.Next(0, 20));
        }
	}
}