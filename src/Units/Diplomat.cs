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
using CivOne.Screens.Services;
using CivOne.Services.Random;

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
			City? capital = cityToIncite.CityOwnerPlayer.GetCapital();

			int distance = capital == null ? 16 : cityToIncite.Tile.DistanceTo(capital);
			
			int cost = (cityToIncite.CityOwnerPlayer.Gold + 1000) / (distance + 3);

			// if city is in disorder need to halve the cost
			CitizenTypes citizenTypes = cityToIncite.GetCitizenTypes();
            if (citizenTypes.InDisorder)
                cost /= 2;

			return cost;
		}

		public IAdvance? GetAdvanceToSteal(Player victim)
		{
			IList<IAdvance> possible = [.. victim.Advances.Where(p => !Player.Advances.Any(p2 => p2.Id == p.Id))];

			if (!possible.Any())
				return null;

			return _randomService.NextElement(possible);
		}

		public string Sabotage(City city)
		{
			Game.DisbandUnit(this);

			IList<IBuilding> buildings = [.. city.Buildings.Where(b => b.GetType() != typeof(Buildings.Palace))];

			int random = _randomService.NextInt(buildings.Count + 1); // +1 to allow for the possibility of sabotaging production instead of a building

			if (random == buildings.Count)
			{
				city.Shields = 0;
				string? production = (city.CurrentProduction as ICivilopedia)?.TranslatedName;
				
				if (string.IsNullOrEmpty(production)) return Translate("City production sabotaged");
				return TranslateFormatted("City production sabotaged: {0}", production);
			}
		
			city.RemoveBuilding(buildings[random]);
			return TranslateFormatted("{0} sabotaged", buildings[random].TranslatedName);
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
                else if (Human == targetCity.CityOwnerPlayer)
                {
                    GameTask.Enqueue(Message.Spy(TranslateFormatted("Spies report: {0} in {1}", Sabotage(targetCity), targetCity.Name)));
                }
				else
					Sabotage(targetCity);
					
				return true;
			}

			IUnit[] enemies = [.. moveTarget.Units.Where(u => u.Owner != Owner)];

			if (enemies.Length > 0)
			{
				if (Human == Owner && enemies.Length == 1 && enemies[0] is BaseUnitLand unitLand)
					GameTask.Enqueue(Show.DiplomatBribe(unitLand, this));
				return false;
			}

			MovementTo(relX, relY);
			return true;
		}

		internal void KeepMoving(IUnit unit) => MovementTo(unit.X - X, unit.Y - Y);

		private readonly IRandomService _randomService;

		public Diplomat() : base(3, 0, 0, 2)
		{
			_randomService = RandomServiceFactory.Create();
			Type = UnitType.Diplomat;
			Name = "Diplomat";
			TranslatedName = Translate("Diplomat");
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
			WorkProgress = (byte)(10 + _randomService.NextByte(20));
        }
	}
}