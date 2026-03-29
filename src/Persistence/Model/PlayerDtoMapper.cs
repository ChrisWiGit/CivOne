using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;

namespace CivOne.Persistence.Model
{
	using PlayerId = System.UInt32;
    using AdvanceId = System.UInt32;
    using GovernmentId = System.Byte;
	using CityId = System.UInt32;

	public interface IPlayerFactory
	{
		/// <summary>
		/// Creates or reassigns a player to a civilization. 
		/// </summary>
		/// <param name="civilization"></param>
		/// <returns></returns>
		IPlayer Create(ICivilization civilization, PlayerDto dto);
	}

    public class PlayerDtoMapper(
		IPlayerGame gameInstance,
		IPlayerFactory _playerFactory,
		DtoMapper<CivilizationDto, ICivilization> _civilizationMapper,
		PalaceDtoMapper _palaceMapper,
		CityDtoMapper _cityMapper,
		UnitDtoMapper _unitMapper
		) : DtoMapper<PlayerDto, IPlayer>
	{

		public IPlayer FromDto(PlayerDto dto)
		{
			// must be set by Create()
			// if (Player.Game == null) {
			// 	// TODO: remove if we can inject the game instance into the player by constructor
			// 	// not good here. should be assigned by higher abstraction layer
			// 	Player.Game = gameInstance;
			// }
			
			var civilization = _civilizationMapper.FromDto(dto.Civilization);
			return _playerFactory.Create(civilization, dto);
		}
        public PlayerDto ToDto(IPlayer player)
		{
			// Find the player's index in the game to use for unit filtering
			int playerIndex = Validate(player);

			return new PlayerDto
			{
				Civilization = _civilizationMapper.ToDto(player.Civilization),

				Explored = player.Explored,
				Visible = player.Visible,

				TribeName = player.TribeName,
				TribeNamePlural = player.TribeNamePlural,

				Advances = [.. player.Advances.Select(a => (AdvanceId)a)],
				Embassies = [.. player.Embassies],

				Anarchy = player.Anarchy,
				Gold = player.Gold,
				CurrentResearch = (AdvanceId)player.CurrentResearch?.Id,
				CityNamesSkipped = player.CityNamesSkipped,

				Government = player.Government.Id,
				LuxuriesRate = player.LuxuriesRate,
				TaxesRate = player.TaxesRate,
				ScienceRate = player.ScienceRate,
				Science = player.Science,
				Palace = _palaceMapper.ToDto(player.Palace),

				Cities = [.. player.Cities
					.Select(_cityMapper.ToDto)],

				// Filter units by owner ID (byte) to avoid instance comparison issues
				Units = [.. gameInstance.GetUnits()
					.Where(u => u.Owner == (byte)playerIndex)
					.Select(_unitMapper.ToDto)]
			};
		}

		private int Validate(IPlayer player)
		{
			var playerIndex = gameInstance.Players.ToList().IndexOf((Player)player);
			if (playerIndex < 0)
				throw new InvalidOperationException($"Player {player.TribeName} not found in game instance");
			return playerIndex;
		}
	}
}


