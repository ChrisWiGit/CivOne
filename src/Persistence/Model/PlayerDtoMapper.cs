using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;

namespace CivOne.Persistence.Model
{
	using AdvanceId = System.UInt32;

    public class PlayerDtoMapper(
		IPlayerGame gameInstance,
		IPlayerOwnerResolver ownerResolver,
		IPlayerFactory _playerFactory,
		DtoMapper<CivilizationDto, ICivilization> _civilizationMapper,
		PalaceDtoMapper _palaceMapper,
		CityDtoMapper _cityMapper,
		UnitDtoMapper _unitMapper
		) : DtoMapper<PlayerDto, IPlayer>
	{

		public IPlayer FromDto(PlayerDto dto)
		{
			if (dto == null)
			{
				throw new ArgumentNullException(nameof(dto));
			}

			// must be set by Create()
			// if (Player.Game == null) {
			// 	// TODO: remove if we can inject the game instance into the player by constructor
			// 	// not good here. should be assigned by higher abstraction layer
			// 	Player.Game = gameInstance;
			// }
			
			var civilization = _civilizationMapper.FromDto(dto.Civilization);
			
			IPlayerRestorable player = _playerFactory.Create(civilization, dto);
			
			player.TribeName = dto.TribeName;
			player.TribeNamePlural = dto.TribeNamePlural;
			player.Explored = dto.Explored;
			player.Visible = dto.Visible;
			player.Advances = [.. (dto.Advances ?? []).Select(x => (byte)x)];
			player.Embassies = [.. (dto.Embassies ?? []).Select(x => (byte)x)];
			player.Anarchy = dto.Anarchy;
			player.Gold = (short)Math.Clamp(dto.Gold, short.MinValue, short.MaxValue);
			player.CurrentResearch = Common.Advances.FirstOrDefault(x => x.Id == dto.CurrentResearch);
			player.CityNamesSkipped = dto.CityNamesSkipped;
			player.Government = Reflect.GetGovernments().FirstOrDefault(x => x.Id == dto.Government);

			// Keep rate invariant (luxuries + taxes + science == 10) by setting all three.
			player.TaxesRate = dto.TaxesRate;
			player.LuxuriesRate = dto.LuxuriesRate;
			player.ScienceRate = dto.ScienceRate;
			player.Science = (short)Math.Clamp(dto.Science, short.MinValue, short.MaxValue);

			if (dto.Palace != null)
			{
				player.Palace = _palaceMapper.FromDto(dto.Palace);
			}

			return player;
		}
        public PlayerDto ToDto(IPlayer player)
		{
			var hasOwnerId = ownerResolver.TryResolveOwnerId(player, out var ownerId);

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
					.Where(u => !hasOwnerId || u.Owner == ownerId)
					.Select(_unitMapper.ToDto)]
			};
		}
	}
}


