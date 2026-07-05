using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Screens.Services
{
	[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "The collections are initialized in the constructor and then modified by adding/removing items, but the property itself is not intended to be read-only.")]
	[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This struct is not intended to be used in equality comparisons or as a key in hash-based collections.")]
	[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The collections need to be List<T> for internal use and are not intended to be exposed as read-only interfaces.")]
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

		[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The list is intended for internal use and is not exposed as a public API.")]
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