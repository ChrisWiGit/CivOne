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
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Tiles;

// KBR 20200927 integrate cdonges land spawn code

namespace CivOne.Civilizations
{
	internal class Barbarian : BaseCivilization<Atilla>
	{
		internal static readonly byte Owner;

		/// <summary>
		/// Tells whether the current turn is one where barbarians appear.
		/// Land and sea raiders share the same rhythm, so this answers for both.
		/// Whether they may actually appear is a separate question, see <see cref="BarbarianSpawnDelegate"/>.
		/// </summary>
		internal static bool IsSpawnTurn => Game.Started && (Game.GameTurn % 8 == 0) && (Game.GameTurn > 150 || Game.GameTurn >= (5 - Game.Difficulty) * 32) && !Game.Players.Any(x => x.HasAdvance<Combustion>());

		internal static bool IsSeaSpawnTurn => IsSpawnTurn;

		// TODO land spawn rate
		internal static bool IsLandSpawnTurn => IsSpawnTurn; // no idea - make them the same for now

		/// <summary>
		/// The units of a raiding party arriving by sea, for the current turn.
		/// Whether a party arrives at all is decided by <see cref="BarbarianSpawnDelegate"/>.
		/// </summary>
		internal static IEnumerable<UnitType> SeaSpawnUnits
		{
			get
			{
				yield return (Game.GameTurn < 300) ? UnitType.Sail : UnitType.Frigate;
				
				UnitType unitType = (Game.Players.Any(x => x.HasAdvance<Gunpowder>())) ? UnitType.Knights : UnitType.Legion;
				int unitCount = (Game.GameTurn < 150) ? 1 : (Game.GameTurn < 300) ? 2 : 3;
				for (int i = 0; i < unitCount; i++)
					yield return unitType;
				yield return UnitType.Diplomat;
			}
		}

		// https://forums.civfanatics.com/threads/barbarians-spawn-logic.630389/#post-15096489
		/// <summary>
		/// The units of a raiding party appearing inland, for the current turn.
		/// Whether a party appears at all is decided by <see cref="BarbarianSpawnDelegate"/>.
		/// </summary>
		internal static IEnumerable<UnitType> LandSpawnUnits
		{
			get
			{
				// TODO this doesn't look right for land units
				//yield return (Game.GameTurn < 300) ? UnitType.Sail : UnitType.Frigate;

				UnitType unitType = (Game.Players.Any(x => x.HasAdvance<Gunpowder>())) ? UnitType.Knights : UnitType.Legion;
				int unitCount = (Game.GameTurn < 150) ? 1 : (Game.GameTurn < 300) ? 2 : 3;
				for (int i = 0; i < unitCount; i++)
					yield return unitType;
				yield return UnitType.Diplomat;
			}
		}

		internal static ITile? SeaSpawnPosition
		{
			get
			{
				IRandomService random = RandomServiceFactory.Create();
				ITile[] tiles = [.. Map.AllTiles().Where(t => t != null && t.IsOcean)];
				for (int i = 0; i < 1000; i++)
				{
					ITile tile = tiles[random.NextInt(tiles.Length)];
					if (tile == null || !tile.IsOcean || tile.GetBorderTiles().Any(t => t == null || !t.IsOcean)) continue;
					return tile;
				}
				return null;
			}
		}

		internal static ITile? LandSpawnPosition
		{
			get
			{
				IRandomService random = RandomServiceFactory.Create();
				// CW: don't spawn barbarians on cities itself.
				ITile[] tiles = [.. Map.AllTiles().Where(t => t != null && !t.IsOcean && t.Visited != 0 && t.City == null)];
				for (int i = 0; i < 1000; i++)
				{
					ITile tile = tiles[random.NextInt(tiles.Length)];
					if (tile == null || Game.GetCities().Any(c => c.CityTiles.Any(t => t == tile))) continue;
					return tile;
				}
				return null;
			}
		}

		public Barbarian() : base(Civilization.Barbarians, TranslationServiceFactory.GetCurrent().Translate("Barbarian"), TranslationServiceFactory.GetCurrent().Translate("Barbarians"))
		{
			StartX = 255;
			StartY = 255;
			CityNames = TranslateArray(
				"Mecca\n" +
				"Naples\n" +
				"Sidon\n" +
				"Tyre\n" +
				"Tarsus\n" +
				"Issus\n" +
				"Cunaxa\n" +
				"Cremona\n" +
				"Cannae\n" +
				"Capua\n" +
				"Turin\n" +
				"Genoa\n" +
				"Utica\n" +
				"Crete\n" +
				"Damascus\n" +
				"Verona\n" +
				"Salamis\n" +
				"Lisbon\n" +
				"Hamburg\n" +
				"Prague\n" +
				"Salzburg\n" +
				"Bergen\n" +
				"Venice\n" +
				"Milan\n" +
				"Ghent\n" +
				"Pisa\n" +
				"Cordoba\n" +
				"Seville\n" +
				"Dublin\n" +
				"Toronto\n" +
				"Melbourne\n" +
				"Sydney");
		}
	}
}