using System;
using System.Collections.Generic;
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
			
			player.PlayerGuid = dto.PlayerGuid == Guid.Empty ? Guid.NewGuid() : dto.PlayerGuid;
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
			player.Diplomacy = BuildDiplomacyArray(dto.Diplomacy);
			player.Anarchy = dto.Anarchy;
			player.Gold = _yamlReadValueSanitizer.ClampToInt16(dto.Gold, nameof(PlayerDtoMapper), nameof(PlayerDto.Gold));
			player.CurrentResearch = _advanceResolver.ResolveById(dto.CurrentResearch);
			player.CityNamesSkipped = dto.CityNamesSkipped;
			player.FutureTechCount = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.FutureTechCount, nameof(PlayerDtoMapper), nameof(PlayerDto.FutureTechCount), min: 0, max: ushort.MaxValue);
			player.HumanContactTurn = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.HumanContactTurn, nameof(PlayerDtoMapper), nameof(PlayerDto.HumanContactTurn), min: 0, max: ushort.MaxValue);
			player.StartX = _yamlReadValueSanitizer.ClampToInt16(dto.StartX, nameof(PlayerDtoMapper), nameof(PlayerDto.StartX));
			player.UnitsLost = BuildUnitsLostArray(dto.UnitsLost);
			player.UnitsDestroyedBy = BuildUnitsDestroyedByArray(dto.UnitsDestroyedBy);
			player.EpicRanking = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.EpicRanking, nameof(PlayerDtoMapper), nameof(PlayerDto.EpicRanking), min: 0, max: ushort.MaxValue);
			player.MilitaryPower = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.MilitaryPower, nameof(PlayerDtoMapper), nameof(PlayerDto.MilitaryPower), min: 0, max: ushort.MaxValue);
			player.CivilizationScore = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.CivilizationScore, nameof(PlayerDtoMapper), nameof(PlayerDto.CivilizationScore), min: 0, max: ushort.MaxValue);
			player.Government = _governmentResolver.ResolveById(dto.Government);

			// Spaceship state
			if (dto.SpaceShip != null)
			{
				player.SpaceShipGrid = dto.SpaceShip.Grid?.ToArray() ?? new CivOne.Enums.SpaceShipComponentType[12, 12];
				player.SpaceShipPopulation = (ushort)_yamlReadValueSanitizer.ClampToInt32(dto.SpaceShip.Population, nameof(PlayerDtoMapper), $"{nameof(PlayerDto.SpaceShip)}.{nameof(SpaceShipDto.Population)}", min: 0, max: ushort.MaxValue);
				player.SpaceShipLaunchYear = (short)_yamlReadValueSanitizer.ClampToInt32(dto.SpaceShip.LaunchYear, nameof(PlayerDtoMapper), $"{nameof(PlayerDto.SpaceShip)}.{nameof(SpaceShipDto.LaunchYear)}", min: short.MinValue, max: short.MaxValue);
			}

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
				PlayerGuid = player.PlayerGuid,

				Explored = player.Explored,
				Visible = player.Visible,

				TribeName = player.TribeName,
				TribeNamePlural = player.TribeNamePlural,

				Advances = [.. player.Advances.Select(a => (AdvanceId)a)],
				Embassies = [.. player.Embassies],
				Diplomacy = [.. player.Diplomacy.Select((flags, targetId) => new DiplomacyEntryDto
				{
					TargetPlayerId = (ushort)targetId,
					RawFlags = flags,
					Decoded = new DiplomacyDecodedDto()
				})],

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
				UnitsLost = [.. player.UnitsLost.Select(x => (long)x)],
				UnitsDestroyedBy = [.. player.UnitsDestroyedBy.Select(x => (long)x)],
				EpicRanking = player.EpicRanking,
				MilitaryPower = player.MilitaryPower,
				CivilizationScore = player.CivilizationScore,
				Palace = _palaceMapper.ToDto(player.Palace),
				SpaceShip = BuildSpaceShipDto(player),

				Cities = [.. player.Cities
					.Select(_cityMapper.ToDto)],

				// Filter units by owner ID (byte) to avoid instance comparison issues
				Units = [.. gameInstance.GetUnits()
					.Where(u => !hasOwnerId || u.Owner == ownerId)
					.Select(_unitMapper.ToDto)]
			};
		}

		private SpaceShipDto BuildSpaceShipDto(IPlayer player)
		{
			if (player is not IPlayerRestorable restorablePlayer)
			{
				return new SpaceShipDto
				{
					Grid = new SpaceShipGridMap2d(new CivOne.Enums.SpaceShipComponentType[12, 12]),
					Population = 0,
					LaunchYear = 0
				};
			}

			var grid = restorablePlayer.SpaceShipGrid ?? new CivOne.Enums.SpaceShipComponentType[12, 12];

			return new SpaceShipDto
			{
				Grid = new SpaceShipGridMap2d(grid),
				Population = restorablePlayer.SpaceShipPopulation,
				LaunchYear = restorablePlayer.SpaceShipLaunchYear
			};
		}

		private ushort[] BuildDiplomacyArray(List<DiplomacyEntryDto> entries)
		{
			var diplomacy = new ushort[8];
			if (entries == null)
			{
				return diplomacy;
			}

			foreach (var entry in entries)
			{
				if (entry == null)
				{
					continue;
				}

				var target = _yamlReadValueSanitizer.ClampToInt32(
					entry.TargetPlayerId,
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.Diplomacy)}.{nameof(DiplomacyEntryDto.TargetPlayerId)}",
					min: 0,
					max: diplomacy.Length - 1);

				diplomacy[target] = (ushort)_yamlReadValueSanitizer.ClampToInt32(
					entry.RawFlags,
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.Diplomacy)}.{nameof(DiplomacyEntryDto.RawFlags)}",
					min: 0,
					max: ushort.MaxValue);
			}

			return diplomacy;
		}

		private ushort[] BuildUnitsLostArray(List<long> values)
		{
			var output = new ushort[28];
			if (values == null)
			{
				return output;
			}

			for (var i = 0; i < output.Length && i < values.Count; i++)
			{
				output[i] = (ushort)_yamlReadValueSanitizer.ClampToInt32(
					values[i],
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.UnitsLost)}[{i}]",
					min: 0,
					max: ushort.MaxValue);
			}

			return output;
		}

		private ushort[] BuildUnitsDestroyedByArray(List<long> values)
		{
			var output = new ushort[8];
			if (values == null)
			{
				return output;
			}

			for (var i = 0; i < output.Length && i < values.Count; i++)
			{
				output[i] = (ushort)_yamlReadValueSanitizer.ClampToInt32(
					values[i],
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.UnitsDestroyedBy)}[{i}]",
					min: 0,
					max: ushort.MaxValue);
			}

			return output;
		}
	}
}


