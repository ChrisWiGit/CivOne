// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using PersistencePlayer = CivOne.Persistence.Game.IPlayer;

namespace CivOne.Screens.Reports
{
	public enum CivilizationRankingCategory
	{
		Richest,
		Strongest,
		MostAdvanced,
		Happiest,
		Largest
	}

	public enum CivilizationRankingHistorian
	{
		Herodotus,
		Pliny,
		Toynbee,
		Gibbon
	}

	public readonly record struct CivilizationRankingRow(int RankNumber, string CivilizationName, IPalaceData PalaceData, byte PlayerColorId);

	public interface ICivilizationRankingService
	{
		IReadOnlyList<CivilizationRankingRow> GetRichest(bool includeAllCivilizations = false);
		IReadOnlyList<CivilizationRankingRow> GetStrongest(bool includeAllCivilizations = false);
		IReadOnlyList<CivilizationRankingRow> GetMostAdvanced(bool includeAllCivilizations = false);
		IReadOnlyList<CivilizationRankingRow> GetHappiest(bool includeAllCivilizations = false);
		IReadOnlyList<CivilizationRankingRow> GetLargest(bool includeAllCivilizations = false);
	}

	public static class CivilizationRankingServiceFactory
	{
		private static ICivilizationRankingService _instance;

		public static ICivilizationRankingService GetInstance()
		{
			if (_instance != null)
			{
				return _instance;
			}

			_instance = new CivilizationRankingService();
			return _instance;
		}

		public static void ClearInstance() => _instance = null;
	}

	public sealed class CivilizationRankingService : ICivilizationRankingService
	{
		public IReadOnlyList<CivilizationRankingRow> GetRichest(bool includeAllCivilizations = false) => RankBy(player => player.Gold, includeAllCivilizations);

		public IReadOnlyList<CivilizationRankingRow> GetStrongest(bool includeAllCivilizations = false) => RankBy(GetStrongScore, includeAllCivilizations);

		public IReadOnlyList<CivilizationRankingRow> GetMostAdvanced(bool includeAllCivilizations = false) => RankBy(player =>
		{
			PersistencePlayer state = player;
			return player.Advances.Length + (state.FutureTechCount * 10);
		}, includeAllCivilizations);

		public IReadOnlyList<CivilizationRankingRow> GetHappiest(bool includeAllCivilizations = false) => RankBy(GetHappinessScore, includeAllCivilizations);

		public IReadOnlyList<CivilizationRankingRow> GetLargest(bool includeAllCivilizations = false) => RankBy(player => player.Population, includeAllCivilizations);

		private static IReadOnlyList<CivilizationRankingRow> RankBy(Func<Player, double> scoreSelector, bool includeAllCivilizations)
		{
			Game game = Game.Instance;
			Player[] allPlayers = [.. GetPlayers(includeAllCivilizations: true)];
			Player[] rankedPlayers = [.. allPlayers
				.OrderByDescending(scoreSelector)
				.ThenBy(player => game.PlayerNumber(player))];

			IReadOnlySet<byte> visiblePlayerIds = includeAllCivilizations
				? new HashSet<byte>(allPlayers.Select(game.PlayerNumber))
				: new HashSet<byte>(GetPlayers(includeAllCivilizations: false).Select(game.PlayerNumber));

			List<CivilizationRankingRow> rows = [];
			for (int i = 0; i < rankedPlayers.Length; i++)
			{
				Player player = rankedPlayers[i];
				byte playerId = game.PlayerNumber(player);
				if (!visiblePlayerIds.Contains(playerId))
				{
					continue;
				}

				rows.Add(new CivilizationRankingRow(
					i + 1,
					player.TribeNamePlural,
					player.Palace,
					playerId));
			}

			return rows;
		}

		private static IEnumerable<Player> GetPlayers(bool includeAllCivilizations)
		{
			Game game = Game.Instance;
			Player humanPlayer = game.HumanPlayer;
			foreach (Player player in game.Players)
			{
				if (player == null || player.Civilization is Barbarian || player.IsDestroyed)
				{
					continue;
				}

				if (includeAllCivilizations || humanPlayer == null || player == humanPlayer)
				{
					yield return player;
					continue;
				}

				PersistencePlayer state = player;
				bool encountered =
					state.HumanContactTurn > 0 ||
					humanPlayer.HasEmbassy(player) ||
					player.HasEmbassy(humanPlayer);
				if (encountered)
				{
					yield return player;
				}
			}
		}

		private static double GetStrongScore(Player player)
		{
			Game game = Game.Instance;
			PersistencePlayer state = player;
			double militaryPower = state.MilitaryPower;
			if (militaryPower > 0)
			{
				return militaryPower;
			}

			byte playerNumber = game.PlayerNumber(player);
			int militaryUnitCount = game.GetUnits().Count(unit => unit.Owner == playerNumber);
			return (militaryUnitCount * 1000) + (player.Cities.Length * 10) + player.Population;
		}

		private static double GetHappinessScore(Player player)
		{
			City[] cities = player.Cities;
			int totalCitizens = 0;
			int happyCitizens = 0;
			int unhappyCitizens = 0;

			foreach (City city in cities)
			{
				foreach (Citizen citizen in city.GetCitizens())
				{
					totalCitizens++;
					if (citizen == Citizen.HappyMale || citizen == Citizen.HappyFemale)
					{
						happyCitizens++;
					}
					else if (citizen == Citizen.UnhappyMale || citizen == Citizen.UnhappyFemale)
					{
						unhappyCitizens++;
					}
				}
			}

			if (totalCitizens == 0)
			{
				return double.MinValue;
			}

			double happinessRatio = (double)happyCitizens / totalCitizens;
			double unhappinessRatio = (double)unhappyCitizens / totalCitizens;
			return (happinessRatio * 1000) - (unhappinessRatio * 100) + totalCitizens;
		}
	}
}