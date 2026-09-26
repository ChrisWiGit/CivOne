namespace CivOne.Persistence.Model
{
	using System;
	using System.Linq;
	using CivOne.Persistence.Game;

	public sealed class CityDebugSnapshotWrapper(IPlayerGame? playerGame = null, ISettings? settings = null)
	{
		private readonly IPlayerGame? _playerGame = playerGame;
		private readonly ISettings? _settings = settings;

		public CityDebugSnapshotDto? Create(ICityMapper domain)
		{
			if (_settings?.DebugMenu != true)
			{
				return null;
			}

			if (domain is not City city)
			{
				return null;
			}

			var citizenTypes = city.GetCitizenTypes();
			int unitsInCityTile = city.Tile.Units.Length;
			int homeUnitsTotal = 0;
			int homeUnitsInCityTile = 0;

			bool gameStarted = _playerGame?.Started == true;
			int difficulty = gameStarted ? _playerGame!.Difficulty : 0;

			if (gameStarted)
			{
				var homeUnits = _playerGame!.GetUnits().Where(u => u.IsHome(city)).ToArray();
				homeUnitsTotal = homeUnits.Length;
				homeUnitsInCityTile = homeUnits.Count(u => u.X == city.X && u.Y == city.Y);
			}

			int? baseUnhappyRaw = null;
			int? empireSizeBase = null;
			int? empireSizePenalty = null;

			bool isHumanOwner = IsHumanOwner(_playerGame, city);
			int cityIndex = ResolveCityIndex(_playerGame, city);
			int cityCount = ResolveCityCount(city);

			if (Settings.Instance.OriginalHappinessModel && gameStarted)
			{
				empireSizeBase = ComputeEmpireSizeBase(difficulty, city.PlayerIntf.Government.Id);
				empireSizePenalty = isHumanOwner ? ComputeEmpireSizePenalty(cityIndex, cityCount, empireSizeBase.Value) : 0;
				baseUnhappyRaw = isHumanOwner
					? city.Size + difficulty - 6 + empireSizePenalty
					: city.Size - 3;
			}

			return new CityDebugSnapshotDto
			{
				OriginalHappinessModelEnabled = Settings.Instance.OriginalHappinessModel,
				Difficulty = difficulty,
				GovernmentId = city.PlayerIntf.Government.Id,
				IsHumanOwner = isHumanOwner,
				CityIndex = cityIndex,
				LuxuriesRate = city.PlayerIntf.LuxuriesRate,
				TaxesRate = city.PlayerIntf.TaxesRate,
				ScienceRate = city.PlayerIntf.ScienceRate,
				FinalHappy = citizenTypes.happy,
				FinalContent = citizenTypes.content,
				FinalUnhappy = citizenTypes.unhappy,
				FinalRedShirt = citizenTypes.redShirt,
				Specialists = city.Specialists.Length,
				InDisorder = citizenTypes.InDisorder,
				UnitsInCityTile = unitsInCityTile,
				HomeUnitsTotal = homeUnitsTotal,
				HomeUnitsInCityTile = homeUnitsInCityTile,
				HomeUnitsOutside = Math.Max(homeUnitsTotal - homeUnitsInCityTile, 0),
				BaseUnhappyRaw = baseUnhappyRaw,
				EmpireSizeBase = empireSizeBase,
				EmpireSizePenalty = empireSizePenalty
			};
		}

		private static bool IsHumanOwner(IPlayerGame? playerGame, City city)
		{
			if (playerGame?.Started != true)
			{
				return false;
			}

			if (city.PlayerIntf is not Player owner)
			{
				return false;
			}

			return ReferenceEquals(playerGame.HumanPlayer, owner);
		}

		private static int ResolveCityCount(City city)
		{
			if (city.PlayerIntf is Player owner)
			{
				return owner.Cities.Length;
			}

			return city.PlayerIntf.CitiesInterface.Length;
		}

		private static int ResolveCityIndex(IPlayerGame? playerGame, City city)
		{
			if (playerGame is IGameCityQuery gameState)
			{
				return Math.Max(gameState.GetCityIndex(city), 0);
			}

			if (city.PlayerIntf is Player owner)
			{
				int index = Array.FindIndex(owner.Cities, c => c.Id == city.Id);
				return Math.Max(index, 0);
			}

			ICity[] cities = city.PlayerIntf.CitiesInterface;
			for (int i = 0; i < cities.Length; i++)
			{
				if (cities[i].Id == city.Id)
				{
					return i;
				}
			}

			return 0;
		}

		private static int ComputeEmpireSizeBase(int difficulty, int governmentId)
		{
			int clampedDifficulty = Math.Clamp(difficulty, 0, 4);
			return Math.Max(((governmentId / 2) + 2) * (7 - clampedDifficulty), 0);
		}

		private static int ComputeEmpireSizePenalty(int cityIndex, int cityCount, int empireSizeBase)
		{
			if (empireSizeBase <= 0)
			{
				return 0;
			}

			return Math.Max(((cityIndex % empireSizeBase) + cityCount - empireSizeBase) / empireSizeBase, 0);
		}
	}
}