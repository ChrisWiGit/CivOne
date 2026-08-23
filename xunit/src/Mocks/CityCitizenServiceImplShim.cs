using CivOne.Enums;
using CivOne.Screens.Services;
using System.Collections.Generic;

namespace CivOne.UnitTests
{

    sealed class CityCitizenServiceImplShim : CityCitizenService
    {
        public CityCitizenServiceImplShim(ICityBasic city,
                ICityBuildings cityBuildings,
                IGameCitizenDependency game,
                List<Citizen> specialists,
                IMap map) : base(city, cityBuildings, game, specialists, map)
        {
        }

		public bool? BachsCathedral { get; set; }

		protected override internal bool HasBachsCathedral()
        {
            if (BachsCathedral.HasValue)
            {
                return BachsCathedral.Value;
            }
            return base.HasBachsCathedral();
        }

		public int? CathedralDeltaValue { get; set; }
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
