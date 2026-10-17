using System;
using CivOne.Enums;
using CivOne.Persistence.Mapper;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Maps the palace between its YAML form and <see cref="PalaceData"/>.
	/// </summary>
	/// <param name="yamlReadValueSanitizer">Clamps values read from YAML.</param>
	/// <param name="logger">Reports defaults used for missing values; <c>null</c> logs nothing.</param>
	public class PalaceDtoMapper(IValueSanitizer yamlReadValueSanitizer, ILogger? logger = null) : IDtoMapper<PalaceDto, PalaceData>
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
				{
					var style = yamlReadValueSanitizer.ClampToByte((int)section.Style, nameof(PalaceDtoMapper), nameof(PalaceSectionDto.Style), min: 0, max: 3);
					var level = yamlReadValueSanitizer.ClampToByte(section.Level, nameof(PalaceDtoMapper), nameof(PalaceSectionDto.Level), min: 0, max: 4);
					palace.SetPalace(i, style, level);
				}
			}
			palace.SetGarden(0, yamlReadValueSanitizer.ClampToByte(dto.GardenLeftLevel, nameof(PalaceDtoMapper), nameof(PalaceDto.GardenLeftLevel), min: 0, max: 3));
			palace.SetGarden(1, yamlReadValueSanitizer.ClampToByte(dto.GardenCenterLevel, nameof(PalaceDtoMapper), nameof(PalaceDto.GardenCenterLevel), min: 0, max: 3));
			palace.SetGarden(2, yamlReadValueSanitizer.ClampToByte(dto.GardenRightLevel, nameof(PalaceDtoMapper), nameof(PalaceDto.GardenRightLevel), min: 0, max: 3));
			palace.SetBuildCount(BuildCountFrom(dto, palace));
			return palace;
		}

		public PalaceDto ToDto(PalaceData palace)
		{
			ArgumentNullException.ThrowIfNull(palace);
			
			var dto = new PalaceDto();
			for (int i = 0; i < _sectionSetters.Length; i++)
				_sectionSetters[i](dto, new PalaceSectionDto(palace.GetPalaceStyle(i), palace.GetPalaceLevel(i)));

			dto.GardenLeftLevel   = palace.GetGardenLevel(0);
			dto.GardenCenterLevel = palace.GetGardenLevel(1);
			dto.GardenRightLevel  = palace.GetGardenLevel(2);
			dto.BuildCount        = palace.BuildCount;
			return dto;
		}

		private int BuildCountFrom(PalaceDto dto, PalaceData palace)
		{
			if (dto.BuildCount is int buildCount)
			{
				return yamlReadValueSanitizer.ClampToInt32(buildCount, nameof(PalaceDtoMapper), nameof(PalaceDto.BuildCount), min: 0, max: short.MaxValue);
			}

			// Older saves have no build counter; each built level stands for one earned upgrade.
			logger?.Log("YAML palace has no build counter, using the built levels ({0}).", palace.UpgradeCount);
			return palace.UpgradeCount;
		}
	}
}
