using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Game;
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Resolver;

namespace CivOne.Persistence.Model
{
	using AdvanceId = UInt32;

	public interface IAdvanceResolver
	{
		IAdvance ResolveById(uint id);
		/// <summary>
		/// Resolves the IDs of all advances in the game. Used to support the "all advances" sentinel value.
		/// </summary>
		IEnumerable<byte> ResolveAllIds();
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
		IValueSanitizer _valueSanitizer
		) : DtoMapper<PlayerDto, IPlayer>
	{
		private const long AllAdvancesSentinel = -1;

		public IPlayer FromDto(PlayerDto dto)
		{
			ArgumentNullException.ThrowIfNull(dto);

			var civilization = _civilizationMapper.FromDto(dto.Civilization);
			
			IPlayerRestorable player = _playerFactory.Create(civilization, dto);
			
			player.PlayerGuid = dto.PlayerGuid == Guid.Empty ? Guid.NewGuid() : dto.PlayerGuid;
			player.TribeName = string.IsNullOrEmpty(dto.TribeName) ? civilization.Name : dto.TribeName;
			player.TribeNamePlural = string.IsNullOrEmpty(dto.TribeNamePlural) ? civilization.NamePlural : dto.TribeNamePlural;
			player.Explored = dto.Explored;
			player.Visible = dto.Visible;
			player.Advances = BuildAdvances(dto.Advances);
			player.Embassies = [..
				(dto.Embassies ?? [])
					.Select(x => _valueSanitizer.ClampToByte(x, nameof(PlayerDtoMapper), nameof(PlayerDto.Embassies)))
			];
			player.Diplomacy = BuildDiplomacyArray(dto.Diplomacy);
			player.Anarchy = dto.Anarchy;
			player.Gold = _valueSanitizer.ClampToInt16(dto.Gold, nameof(PlayerDtoMapper), nameof(PlayerDto.Gold));
			player.CurrentResearch = _advanceResolver.ResolveById(dto.CurrentResearch);
			player.CityNamesSkipped = dto.CityNamesSkipped;
			player.FutureTechCount = _valueSanitizer.ClampToUInt16(dto.FutureTechCount, nameof(PlayerDtoMapper), nameof(PlayerDto.FutureTechCount));
			player.HumanContactTurn = _valueSanitizer.ClampToUInt16(dto.HumanContactTurn, nameof(PlayerDtoMapper), nameof(PlayerDto.HumanContactTurn));
			player.StartX = _valueSanitizer.ClampToInt16(dto.StartX, nameof(PlayerDtoMapper), nameof(PlayerDto.StartX));
			player.UnitsLost = BuildUnitsLostArray(dto.UnitsLost);
			player.UnitsDestroyedBy = BuildUnitsDestroyedByArray(dto.UnitsDestroyedBy);
			player.EpicRanking = _valueSanitizer.ClampToUInt16(dto.EpicRanking, nameof(PlayerDtoMapper), nameof(PlayerDto.EpicRanking));
			player.MilitaryPower = _valueSanitizer.ClampToUInt16(dto.MilitaryPower, nameof(PlayerDtoMapper), nameof(PlayerDto.MilitaryPower));
			player.CivilizationScore = _valueSanitizer.ClampToUInt16(dto.CivilizationScore, nameof(PlayerDtoMapper), nameof(PlayerDto.CivilizationScore));
			player.Government = _governmentResolver.ResolveById(dto.Government);

			// Spaceship state
			if (dto.SpaceShip != null)
			{
				player.SpaceShipGrid = dto.SpaceShip.Grid?.ToArray() ?? new SpaceShipComponentType[12, 12];
				player.SpaceShipPopulation = _valueSanitizer.ClampToUInt16(dto.SpaceShip.Population, nameof(PlayerDtoMapper), $"{nameof(PlayerDto.SpaceShip)}.{nameof(SpaceShipDto.Population)}");
				player.SpaceShipLaunchYear = _valueSanitizer.ClampToInt16(dto.SpaceShip.LaunchYear, nameof(PlayerDtoMapper), $"{nameof(PlayerDto.SpaceShip)}.{nameof(SpaceShipDto.LaunchYear)}");
			}

			// Keep rate invariant (luxuries + taxes + science == 10) by setting all three.
			player.TaxesRate = dto.TaxesRate;
			player.LuxuriesRate = dto.LuxuriesRate;
			player.ScienceRate = dto.ScienceRate;
			player.Science = _valueSanitizer.ClampToInt16(dto.Science, nameof(PlayerDtoMapper), nameof(PlayerDto.Science));

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
			var playersByIndex = TryGetPlayersByIndex();

			return new PlayerDto
			{
				Civilization = _civilizationMapper.ToDto(player.Civilization),
				PlayerGuid = player.PlayerGuid,

				Explored = player.Explored,
				Visible = player.Visible,

				TribeName = player.TribeName,
				TribeNamePlural = player.TribeNamePlural,

				Advances = [.. player.Advances],
				Embassies = [.. player.Embassies],
				Diplomacy = [.. player.Diplomacy.Select((flags, targetId) => new DiplomacyEntryDto
				{
					TargetPlayerId = (ushort)targetId,
					TargetPlayerGuid = ResolveTargetPlayerGuid(playersByIndex, targetId, hasOwnerId, ownerId, player.PlayerGuid),
					RawFlags = flags,
					Decoded = new DiplomacyDecodedDto()
				})],

				Anarchy = player.Anarchy,
				Gold = player.Gold,
				CurrentResearch = player.CurrentResearch?.Id ?? 0,
				Government = player.Government?.Id ?? 0,
				LuxuriesRate = player.LuxuriesRate,
				TaxesRate = player.TaxesRate,
				ScienceRate = player.ScienceRate,
				Science = player.Science,
				FutureTechCount = player.FutureTechCount,
				HumanContactTurn = player.HumanContactTurn,
				StartX = player.StartX,
				UnitsLost = [.. player.UnitsLost],
				UnitsDestroyedBy = [.. player.UnitsDestroyedBy],
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

		private Player[] TryGetPlayersByIndex()
		{
			try
			{
				return (gameInstance.Players ?? []).ToArray();
			}
			catch
			{
				return [];
			}
		}

		private static Guid ResolveTargetPlayerGuid(Player[] playersByIndex, int targetId, bool hasOwnerId, byte ownerId, Guid ownerGuid)
		{
			if (hasOwnerId && targetId == ownerId)
			{
				return ownerGuid;
			}

			if (playersByIndex == null || targetId < 0 || targetId >= playersByIndex.Length)
			{
				return Guid.Empty;
			}

			return playersByIndex[targetId]?.PlayerGuid ?? Guid.Empty;
		}

		private List<byte> BuildAdvances(List<long> advances)
		{
			if (advances == null || advances.Count == 0)
			{
				return [];
			}

			if (advances.Contains(AllAdvancesSentinel))
			{
				return [..
					_advanceResolver.ResolveAllIds()
						.Distinct()
						.OrderBy(id => id)];
			}

			return [..
				advances.Select(x => _valueSanitizer.ClampToByte(x, nameof(PlayerDtoMapper), nameof(PlayerDto.Advances)))];
		}

		private SpaceShipDto BuildSpaceShipDto(IPlayer player)
		{
			if (player is not IPlayerRestorable restorablePlayer)
			{
				return new SpaceShipDto
				{
					Grid = new SpaceShipGridMap2D(new SpaceShipComponentType[12, 12]),
					Population = 0,
					LaunchYear = 0
				};
			}

			var grid = restorablePlayer.SpaceShipGrid ?? new SpaceShipComponentType[12, 12];

			return new SpaceShipDto
			{
				Grid = new SpaceShipGridMap2D(grid),
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

				var target = _valueSanitizer.ClampToInt32(
					entry.TargetPlayerId,
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.Diplomacy)}.{nameof(DiplomacyEntryDto.TargetPlayerId)}",
					min: 0,
					max: diplomacy.Length - 1);

				diplomacy[target] = _valueSanitizer.ClampToUInt16(
					entry.RawFlags,
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.Diplomacy)}.{nameof(DiplomacyEntryDto.RawFlags)}"
				);
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
				output[i] = _valueSanitizer.ClampToUInt16(
					values[i],
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.UnitsLost)}[{i}]"
				);
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
				output[i] = _valueSanitizer.ClampToUInt16(
					values[i],
					nameof(PlayerDtoMapper),
					$"{nameof(PlayerDto.UnitsDestroyedBy)}[{i}]"
				);
			}

			return output;
		}
	}
}


