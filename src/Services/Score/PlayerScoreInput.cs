using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Screens.Services;
using CivOne.Services.SpaceShip;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Live <see cref="IScoredCivilization"/> view of a <see cref="Player"/>.
	/// </summary>
	internal sealed class PlayerScoreInput : IScoredCivilization
	{
		private readonly Player _player;
		private readonly ISpaceShipArrivalService _arrivalService;
		private readonly ushort _gameTurn;

		/// <summary>
		/// Creates a view of <paramref name="player"/> in <paramref name="gameTurn"/>.
		/// </summary>
		/// <param name="player">The player to read.</param>
		/// <param name="arrivalService">Decides spaceship arrival and success rate.</param>
		/// <param name="gameTurn">The current game turn, for the spaceship arrival.</param>
		public PlayerScoreInput(Player player, ISpaceShipArrivalService arrivalService, ushort gameTurn)
		{
			ArgumentNullException.ThrowIfNull(player);
			ArgumentNullException.ThrowIfNull(arrivalService);
			_player = player;
			_arrivalService = arrivalService;
			_gameTurn = gameTurn;
		}

		/// <inheritdoc />
		public IReadOnlyList<CityScoreInput> Cities => [.. _player.Cities.Select(ToScoreInput)];

		/// <inheritdoc />
		public int WonderCount => _player.Cities.Sum(city => city.Wonders.Length);

		/// <inheritdoc />
		public int FutureTechCount => _player.FutureTechCount;

		/// <inheritdoc />
		public int SpaceShipPopulation => _player.SpaceShipPopulation;

		/// <inheritdoc />
		public int SpaceShipSuccessRate => _arrivalService.SuccessRate(_player);

		/// <inheritdoc />
		public bool SpaceShipArrivedThisTurn => _arrivalService.ArrivedThisTurn(_player, _gameTurn);

		private static CityScoreInput ToScoreInput(City city)
		{
			CitizenTypes citizens = city.GetCitizenTypes(CivilizationScoreService.ScoringLuxuryRate);
			return new CityScoreInput(city.Size, citizens.happy, citizens.unhappy, citizens.redShirt);
		}
	}
}
