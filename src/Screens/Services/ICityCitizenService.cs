using System.Collections;
using System.Collections.Generic;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Screens.Services
{
	public struct CitizenTypes
	{
		public int happy;
		public int content;
		public int unhappy;
		public int redshirt;
		public int elvis;
		public int einstein;
		public int taxman;

		public Citizen[] Citizens;

		public List<IBuilding> Buildings;
		public List<IWonder> Wonders;

		public List<IUnit> MarshallLawUnits;


		public int Sum()
		{
			return happy + content + unhappy + redshirt + elvis + einstein + taxman;
		}

		public bool Valid()
		{
			return happy >= 0 && content >= 0 && unhappy >= 0;
		}
	}
	public interface ICityCitizenService
	{
		IEnumerable<CitizenTypes> EnumerateCitizens();
		static ICityCitizenLayoutService Create(City city) => new CityCitizenServiceImpl(city);
	}

}