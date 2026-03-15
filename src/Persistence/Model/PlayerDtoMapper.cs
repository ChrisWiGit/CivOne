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

	public interface PlayerFactory
	{
		IPlayer Create(ICivilization civilization);
	}

    public class PlayerDtoMapper(
		IPlayerGame gameInstance,
		DtoMapper<CivilizationDto, ICivilization> civilizationMapper,
		PalaceDtoMapper palaceMapper,
		CityDtoMapper cityMapper
		) : DtoMapper<PlayerDto, IPlayer>
	{
		private readonly DtoMapper<CivilizationDto, ICivilization> 
			_civilizationMapper = civilizationMapper;
		private readonly PalaceDtoMapper _palaceMapper = palaceMapper;
		private readonly CityDtoMapper _cityMapper = cityMapper;

		public IPlayer FromDto(PlayerDto dto)
		{
			if (Player.Game == null) {
				// TODO: remove if we can inject the game instance into the player by constructor
				Player.Game = gameInstance;
			}
			return new Player(
				civilization: _civilizationMapper.FromDto(dto.Civilization)
				// TODO: many more properties to be mapped here
			);
		}
        public PlayerDto ToDto(IPlayer player)
        {
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
					.Select(_cityMapper.ToDto)]
			};
        }
	}
}


