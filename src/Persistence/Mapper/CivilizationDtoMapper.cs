using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Mapper;

namespace CivOne.Persistence.Model
{
    public class CivilizationDtoMapper : IDtoMapper<CivilizationDto, ICivilization>
    {
        private readonly IEnumerable<ICivilization> _availableCivilizations;

        public CivilizationDtoMapper(IEnumerable<ICivilization> availableCivilizations)
        {
            if (availableCivilizations == null || !availableCivilizations.Any())
            {
                throw new System.ArgumentException("At least one civilization must be provided.", nameof(availableCivilizations));
            }
            _availableCivilizations = availableCivilizations;
        }

        public ICivilization FromDto(CivilizationDto dto)
        {
            return _availableCivilizations
                .First(c =>
                    c.Leader.GetType().Name == dto.LeaderClassName);
        }

        public CivilizationDto ToDto(ICivilization civ)
        {
            return new CivilizationDto
            {
                LeaderClassName = civ.Leader.GetType().Name
            };
        }
    }

}