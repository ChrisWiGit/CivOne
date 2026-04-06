namespace CivOne.Persistence.Model
{
	using System;
	using System.Diagnostics;
	using System.Drawing;
	using System.Linq;
	using CivOne.Units;
	using CityId = System.UInt32;
	using PlayerId = System.Byte;

	public interface IUnitFactory
	{
		IUnitRestorable Create(string className, PlayerId player, Guid? HomeCityGuid);
	}


	public class UnitDtoMapper(IUnitFactory _unitFactory, IYamlReadValueSanitizer yamlReadValueSanitizer) : DtoMapper<UnitDto, IUnit>
	{
		public IUnit FromDto(UnitDto dto)
		{
			var unit = _unitFactory.Create(dto.ClassName, dto.PlayerId, dto.HomeCityGuid);
			unit.Owner = dto.PlayerId;
			unit.PendingHomeCityGuid = dto.HomeCityGuid;
			var locationX = yamlReadValueSanitizer.ClampToInt32(dto.Location.X, nameof(UnitDtoMapper), nameof(UnitDto.Location));
			var locationY = yamlReadValueSanitizer.ClampToInt32(dto.Location.Y, nameof(UnitDtoMapper), nameof(UnitDto.Location));
			var gotoX = yamlReadValueSanitizer.ClampToInt32(dto.Goto.X, nameof(UnitDtoMapper), nameof(UnitDto.Goto));
			var gotoY = yamlReadValueSanitizer.ClampToInt32(dto.Goto.Y, nameof(UnitDtoMapper), nameof(UnitDto.Goto));

			unit.X = Math.Abs(locationX);
			unit.Y = Math.Abs(locationY);
			unit.Goto = new Point(Math.Abs(gotoX), Math.Abs(gotoY));
			unit.Busy = dto.Busy;
			unit.Veteran = dto.Veteran;
			unit.Sentry = dto.Sentry;
			unit.FortifyActive = dto.FortifyActive;
			unit.Fortify = dto.Fortify;
			unit.FuelOrProgress = dto.FuelOrProgress;
			unit.Fuel = dto.Fuel;
			unit.WorkProgress = dto.WorkProgress;
			unit.order = dto.Order;
			unit.MovesSkip = dto.MovesSkip;
			unit.MovesLeft = dto.MovesLeft;
			unit.PartMoves = dto.PartMoves;

			return unit;
		}

		public UnitDto ToDto(IUnit domain)
		{
			Debug.Assert(domain.X >= 0, "Unit X coordinate cannot be negative");
			Debug.Assert(domain.Y >= 0, "Unit Y coordinate cannot be negative");

			return new UnitDto
			{
				ClassName = domain.GetType().Name,
				PlayerId = domain.Owner,
				Location = new MapLocation((uint)domain.X, (uint)domain.Y),
				Goto = new MapLocation(domain.Goto),
				HomeCityGuid = domain.Home?.Id,
				Busy = domain.Busy,
				Veteran = domain.Veteran,
				Sentry = domain.Sentry,
				FortifyActive = domain.FortifyActive,
				Fortify = domain.Fortify,
				FuelOrProgress = domain.FuelOrProgress,
				Fuel = domain.Fuel,
				WorkProgress = domain.WorkProgress,
				Order = domain.order,
				MovesSkip = domain.MovesSkip,
				MovesLeft = domain.MovesLeft,
				PartMoves = domain.PartMoves,
			};
		}
	}
}