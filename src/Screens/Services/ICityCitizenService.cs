using System;
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
		public int happy { get; set; }
		public int content { get; set; }
		public int unhappy { get; set; }

		public int redShirt { get; set; }
		public int elvis { get; set; }
		public int einstein { get; set; }
		public int taxman { get; set; }

		public readonly bool InDisorder => unhappy + redShirt > happy;

		public Citizen[] Citizens { get; set; }

		public List<IBuilding> Buildings { get; set; }
		public List<IWonder> Wonders { get; set; }

		public List<IUnit> MarshallLawUnits { get; set; }


		public readonly int Sum()
		{
			return happy + content + unhappy + redShirt + elvis + einstein + taxman;
		}

		public readonly bool Valid()
		{
			return happy >= 0 && content >= 0 && unhappy >= 0 && redShirt >= 0 && elvis >= 0 && einstein >= 0 && taxman >= 0;
		}
	}
	public interface ICityCitizenService
	{
		IEnumerable<CitizenTypes> EnumerateCitizens();
		CitizenTypes GetCitizenTypes();

		Citizen[] GetCitizens();

		static ICityCitizenService Create(City city, IGame game, List<Citizen> specialists, Map map)
		{
			ArgumentNullException.ThrowIfNull(game);

			if (game is not IGameCitizenDependency dependency)
			{
				throw new ArgumentException("The provided game does not implement IGameCitizenDependency.", nameof(game));
			}

			return new CityCitizenService(city, city, dependency, specialists, map);
		}
	}

}