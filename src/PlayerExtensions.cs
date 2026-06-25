using System.Linq;
using System.Collections.Generic;

using CivOne.Buildings;

namespace CivOne.src
{
    internal static class PlayerExtensions
    {
        public static IEnumerable<City> GetCities(this Player player) => 
            Game.Instance.GetCities().Where(x => x.CityOwnerPlayerIndex == Game.Instance.PlayerNumber(player) && x.Size > 0);

        public static City? GetCapital(this Player player) => 
            player.GetCities().FirstOrDefault(c => c.HasBuilding<Palace>());

        public static string GetCapitalName(this Player player) =>
            player.GetCapital()?.Name ?? "NONE";

    }
}
