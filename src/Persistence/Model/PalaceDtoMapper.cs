using System;
using CivOne.Enums;

namespace CivOne.Persistence.Model
{
	public class PalaceDtoMapper : DtoMapper<PalaceDto, PalaceData>
	{
		private static readonly Func<PalaceDto, PalaceSectionDto>[] _sectionGetters =
		[
			dto => dto.LeftTower,
			dto => dto.LeftWing,
			dto => dto.LeftAnnex,
			dto => dto.Center,
			dto => dto.RightAnnex,
			dto => dto.RightWing,
			dto => dto.RightTower,
		];

		private static readonly Action<PalaceDto, PalaceSectionDto>[] _sectionSetters =
		[
			(dto, s) => dto.LeftTower  = s,
			(dto, s) => dto.LeftWing   = s,
			(dto, s) => dto.LeftAnnex  = s,
			(dto, s) => dto.Center     = s,
			(dto, s) => dto.RightAnnex = s,
			(dto, s) => dto.RightWing  = s,
			(dto, s) => dto.RightTower = s,
		];

		public PalaceData FromDto(PalaceDto dto)
		{
			var palace = new PalaceData();
			for (int i = 0; i < _sectionGetters.Length; i++)
			{
				var section = _sectionGetters[i](dto);
				if (section != null)
					palace.SetPalace(i, (byte)section.Style, section.Level);
			}
			palace.SetGarden(0, dto.GardenLeftLevel);
			palace.SetGarden(1, dto.GardenCenterLevel);
			palace.SetGarden(2, dto.GardenRightLevel);
			return palace;
		}

		public PalaceDto ToDto(PalaceData palace)
		{
			if (palace == null) return null;
			
			var dto = new PalaceDto();
			for (int i = 0; i < _sectionSetters.Length; i++)
				_sectionSetters[i](dto, new PalaceSectionDto(palace.GetPalaceStyle(i), palace.GetPalaceLevel(i)));

			dto.GardenLeftLevel   = palace.GetGardenLevel(0);
			dto.GardenCenterLevel = palace.GetGardenLevel(1);
			dto.GardenRightLevel  = palace.GetGardenLevel(2);
			return dto;
		}
	}
}
