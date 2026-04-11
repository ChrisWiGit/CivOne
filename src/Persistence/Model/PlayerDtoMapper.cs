using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;

namespace CivOne.Persistence.Model
{
	using AdvanceId = System.UInt32;

	public interface IAdvanceResolver
	{
		IAdvance ResolveById(uint id);
	}

	public interface IGovernmentResolver
	{
		IGovernment ResolveById(byte id);
	}

    public class PlayerDtoMapper(
		IPlayerGame gameInstance,
		IPlayerOwnerResolver ownerResolver,
		IPlayerFactory _playerFactory,
		DtoMapper<CivilizationDto, ICivilization> _civilizationMapper,
		PalaceDtoMapper _palaceMapper,
		CityDtoMapper _cityMapper,
		UnitDtoMapper _unitMapper,
		IAdvanceResolver _advanceResolver,
		IGovernmentResolver _governmentResolver,
		IValueSanitizer _yamlReadValueSanitizer
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
			
			player.TribeName = string.IsNullOrEmpty(dto.TribeName) ? civilization.Name : dto.TribeName;
			player.TribeNamePlural = string.IsNullOrEmpty(dto.TribeNamePlural) ? civilization.NamePlural : dto.TribeNamePlural;
			player.Explored = dto.Explored;
			player.Visible = dto.Visible;
			player.Advances = [..
				(dto.Advances ?? [])
					.Select(x => _yamlReadValueSanitizer.ClampToByte(x, nameof(PlayerDtoMapper), nameof(PlayerDto.Advances)))
			];
			player.Embassies = [..
				(dto.Embassies ?? [])
					.Select(x => _yamlReadValueSanitizer.ClampToByte(x, nameof(PlayerDtoMapper), nameof(PlayerDto.Embassies)))
			];
			player.Anarchy = dto.Anarchy;
			player.Gold = _yamlReadValueSanitizer.ClampToInt16(dto.Gold, nameof(PlayerDtoMapper), nameof(PlayerDto.Gold));
			player.CurrentResearch = _advanceResolver.ResolveById(dto.CurrentResearch);
			player.CityNamesSkipped = dto.CityNamesSkipped;
			player.FutureTechCount = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.FutureTechCount, nameof(PlayerDtoMapper), nameof(PlayerDto.FutureTechCount), min: 0, max: ushort.MaxValue);
			player.HumanContactTurn = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.HumanContactTurn, nameof(PlayerDtoMapper), nameof(PlayerDto.HumanContactTurn), min: 0, max: ushort.MaxValue);
			player.StartX = _yamlReadValueSanitizer.ClampToInt16(dto.StartX, nameof(PlayerDtoMapper), nameof(PlayerDto.StartX));
			player.Government = _governmentResolver.ResolveById(dto.Government);

			// Keep rate invariant (luxuries + taxes + science == 10) by setting all three.
			player.TaxesRate = dto.TaxesRate;
			player.LuxuriesRate = dto.LuxuriesRate;
			player.ScienceRate = dto.ScienceRate;
			player.Science = _yamlReadValueSanitizer.ClampToInt16(dto.Science, nameof(PlayerDtoMapper), nameof(PlayerDto.Science));

			if (dto.Palace != null)
			{
				player.Palace = _palaceMapper.FromDto(dto.Palace);
			}

			player.Cities = [..
				(dto.Cities ?? [])
				.Select(_cityMapper.FromDto)
				.Select(city => city as ICity ?? throw new InvalidOperationException("City mapper must return ICity instances"))
			];

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
			CurrentResearch = (AdvanceId)(player.CurrentResearch?.Id ?? 0),
				Government = player.Government?.Id ?? 0,
				LuxuriesRate = player.LuxuriesRate,
				TaxesRate = player.TaxesRate,
				ScienceRate = player.ScienceRate,
				Science = player.Science,
				FutureTechCount = player.FutureTechCount,
				HumanContactTurn = player.HumanContactTurn,
				StartX = player.StartX,
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


