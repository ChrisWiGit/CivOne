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
using CivOne.Screens.Dialogs;
using CivOne.Tasks;
using CivOne.Tiles;

namespace CivOne.Units
{
	internal class Caravan : BaseUnitLand
	{
		private static string[] WARES = new string[] { "Silk", "Silver", "Wine", "Copper", "Gems", "Dye", "Salt", "Spice" };

		private int TradeGoldBonus(City targetCity)
		{
			// This formula is not confirmed... but it seems close enough

			if (targetCity == null) return 0; // this should not happen

			// set default values if home city is NONE
			int distance = Common.DistanceToTile(-1, -1, targetCity.X, targetCity.Y);
			int tradeHome = 12;

			if (Home != null)
			{
				distance = Home.Tile.DistanceTo(targetCity);
				tradeHome = Home.TradeTotal;
			}

			int tradeTarget = targetCity.TradeTotal;

			float multiplier = 1;
			if (Home != null && Home.Tile.ContinentId == targetCity.Tile.ContinentId) multiplier *= 0.5F;
			if (Owner == targetCity.Owner) multiplier *= 0.5F;
			if (Game.GetPlayer(Owner).HasAdvance<RailRoad>() && Game.GetPlayer(targetCity.Owner).HasAdvance<RailRoad>()) multiplier *= 0.66F;
			if (Game.GetPlayer(Owner).HasAdvance<Flight>() && Game.GetPlayer(targetCity.Owner).HasAdvance<Flight>()) multiplier *= 0.66F;

			return (int)(multiplier * (float)((distance + 10) * (tradeHome + tradeTarget) / 24));
		}

		internal void KeepMoving(City city) => MovementTo(city.X - X, city.Y - Y);

		internal void EstablishTradeRoute(City city)
		{
			string homeName = Home?.Name ?? "NONE";
			string ware = WARES[Common.Random.Next(8)];
			int revenue = TradeGoldBonus(city);
			if (revenue <= 0) revenue = 1; // revenue should at least be 1, I think (needs to be checked)

			GameTask.Insert(Message.General(
				$"{ware} caravan from {homeName}",
				$"arrives in {city.Name}",
				"Trade route established",
				$"Revenue: ${revenue}."));
			Game.GetPlayer(Owner).Gold += (short)revenue;
			Game.DisbandUnit(this);
		}

		internal void HelpBuildWonder(City city)
		{
			city.Shields += 50;
			Game.DisbandUnit(this);
		}

		internal override bool Confront(int relX, int relY)
		{
			ITile moveTarget = Map[X, Y][relX, relY];
			City city = moveTarget.City;
			
			bool hasTargetCity = city != null;
			bool isCityOwner = hasTargetCity && city.Owner == Owner;

            // fire-eggs Caravan can build wonder even in home city
			if (isCityOwner && Human == Owner)
			{
				// TODO fire-eggs this seems like bad design: how to determine whether to show a menu, without activating the menu?
				if (city.IsBuildingWonder || CaravanChoice.AllowEstablishTradeRoute(this, city))
				{
					GameTask.Enqueue(Show.CaravanChoice(this, city));
				}
				else
				{
					MovementTo(relX, relY);
				}
				return true;
			}

            if (!hasTargetCity || city == Home ||
				(isCityOwner && Home != null && moveTarget.DistanceTo(Home) < 10))
			{
				MovementTo(relX, relY);
				return true;
			}

			if (!isCityOwner)
			{
				EstablishTradeRoute(moveTarget.City);
				return true;
			}

			return true;
		}
		
		protected override bool? ConfrontCity(ITile moveTarget, int relX, int relY)
		{
			// fire-eggs Caravan needs to be able to move into owner city
			bool hasTargetCity = moveTarget.City != null;

			// if (moveTarget.City != null && (moveTarget.City.Owner != Owner || this is Caravan))
			if (hasTargetCity)
			{
				return Confront(relX, relY);
			}

			return null;
		}

		public Caravan() : base(5, 0, 1, 1)
		{
			Type = UnitType.Caravan;
			Name = "Caravan";
			RequiredTech = new Trade();
			ObsoleteTech = null;
			SetIcon('E', 0, 1);
            Role = UnitRole.Civilian;
        }
	}
}