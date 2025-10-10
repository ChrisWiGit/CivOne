using System.Collections;
using System.Collections.Generic;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Screens
{
	public class CityCitizenServiceImpl(City city) : ICityCitizenService
	{
		private readonly City _city = city;
		
		private void 

		public IEnumerable<CitizenTypes> EnumerateCitizens()
		{
			CitizenTypes ct = new()
			{
				happy = 0,
				content = 0,
				unhappy = 0,
				redshirt = 0,
				elvis = 0,
				einstein = 0,
				taxman = 0,
				Citizens = new Citizen[_city.Size],
				Buildings = [],
				Wonders = [],
				MarshallLawUnits = []
			};


		}
	}

}