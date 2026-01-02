using CivOne.Screens.Services;
using CivOne.Enums;
using System.Collections.Generic;

namespace CivOne.UnitTests
{

    class CityCitizenServiceImplShim : CityCitizenServiceImpl
    {
        public CityCitizenServiceImplShim(ICityBasic city,
                ICityBuildings cityBuildings,
                IGameCitizenDependency game,
                List<Citizen> specialists,
                IMap map) : base(city, cityBuildings, game, specialists, map)
        {
        }

        public bool? BachsCathedral { get; set; } = null;

        protected override internal bool HasBachsCathedral()
        {
            if (BachsCathedral.HasValue)
            {
                return BachsCathedral.Value;
            }
            return base.HasBachsCathedral();
        }

        public int? CathedralDeltaValue { get; set; } = null;
        internal override int CathedralDelta()
        {
            if (CathedralDeltaValue.HasValue)
            {
                return CathedralDeltaValue.Value;
            }
            return base.CathedralDelta();
        }
    }
}
