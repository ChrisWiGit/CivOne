using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Mapper;

namespace CivOne.Persistence.Model
{
    public class CivilizationDtoMapper : IDtoMapper<CivilizationDto, ICivilization>
    {
        private readonly IEnumerable<ICivilization> _availableCivilizations;

        /// <param name="availableCivilizations">
        /// Note: if this collection is backed by a deferred-execution source that yields fresh instances on
        /// each enumeration (e.g. <see cref="Reflect.GetCivilizations"/>), FromDto correctly returns a fresh
        /// instance per call. With civilization reuse (multiple players sharing a civilization), several
        /// players could otherwise share the same ICivilization/ILeader instance, and the Player constructor
        /// mutates civilization.Leader.Name to disambiguate reused civilizations (see Game.NewGame.cs). Do not
        /// pass a pre-materialized array/list here (e.g. Common.Civilizations) in production wiring.
        /// </param>
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