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
using CivOne.Services.Random;
using CivOne.Tasks;
using CivOne.Tiles;

namespace CivOne.Units
{
	internal class Caravan : BaseUnitLand
	{
		private string GetRandomWareName()
		{
			return RandomServiceFactory.Create().NextInt(8) switch
			{
				0 => Translate("Silk"),
				1 => Translate("Silver"),
				2 => Translate("Wine"),
				3 => Translate("Copper"),
				4 => Translate("Gems"),
				5 => Translate("Dye"),
				6 => Translate("Salt"),
				_ => Translate("Spice")
			};
		}

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
			if (Owner == targetCity.CityOwnerPlayerIndex) multiplier *= 0.5F;
			if (OwnerPlayer.HasAdvance<RailRoad>() && targetCity.PlayerIntf.HasAdvance<RailRoad>()) multiplier *= 0.66F;
			if (OwnerPlayer.HasAdvance<Flight>() && targetCity.PlayerIntf.HasAdvance<Flight>()) multiplier *= 0.66F;

			return (int)(multiplier * ((distance + 10) * (tradeHome + tradeTarget) / 24));
		}
		private Player OwnerPlayer => Game.GetPlayer(Owner)!;

		internal void KeepMoving(City city) => MovementTo(city.X - X, city.Y - Y);

		internal void EstablishTradeRoute(City city)
		{
			string homeName = Home?.Name ?? Translate("NONE");
			string ware = GetRandomWareName();
			int revenue = TradeGoldBonus(city);
			if (revenue <= 0) revenue = 1; // revenue should at least be 1, I think (needs to be checked)

			// CW: At first we just add this city to be shown.
			// There are a lot more rules about trade routes
			// In this way we can check if save and load of routes works
			Home?.AddTradingCity(city);

			if (Human == Owner)
			{
				GameTask.Insert(Message.General(
					TranslateFormattedArray("{0} caravan from {1}\narrives in {2}\nTrade route established\nRevenue: ${3}.", ware, homeName, city.Name, revenue)));
			}
			OwnerPlayer.Gold += (short)revenue;
			Game.DisbandUnit(this);
		}

		internal void HelpBuildWonder(City city)
		{
			city.Shields += 50;
			Game.DisbandUnit(this);
		}

		internal override bool PreConfront(int relX, int relY)
		{
			ITile moveTarget = Map[X, Y][relX, relY];

			if (moveTarget.City is not { } city)
			{
				MovementTo(relX, relY);
				return true;
			}

			bool isCityOwner = city.CityOwnerPlayerIndex == Owner;

			if (isCityOwner && Human == Owner)
			{
				if (city.IsBuildingWonder || Home == null || Home.Tile.DistanceTo(city) >= 10)
				{
					GameTask.Enqueue(Show.CaravanChoice(this, city));
				}
				else
				{
					MovementTo(relX, relY);
				}

				return true;
			}

			if (city == Home ||
				(isCityOwner && Home != null && moveTarget.DistanceTo(Home) < 10))
			{
				MovementTo(relX, relY);
				return true;
			}

			if (!isCityOwner)
			{
				EstablishTradeRoute(city);
				return true;
			}

			return true;
		}
		
		protected override bool? ConfrontCity(ITile moveTarget, int relX, int relY)
		{
			bool hasTargetCity = moveTarget.City != null;

			if (hasTargetCity)
			{
				return PreConfront(relX, relY);
			}

			return null;
		}

		public Caravan() : base(5, 0, 1, 1)
		{
			Type = UnitType.Caravan;
			Name = "Caravan";
			TranslatedName = Translate("Caravan");
			RequiredTech = new Trade();
			ObsoleteTech = null;
			SetIcon('E', 0, 1);
            Role = UnitRole.Civilian;
        }
	}
}