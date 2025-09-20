using System;
using System.Collections.Generic;
using CivOne.Enums;

namespace CivOne.Screens
{
	public class CityCitizenLayoutServiceImpl(City city) : ICityCitizenLayoutService
	{
		private readonly City _city = city ?? throw new ArgumentNullException(nameof(city));

		public bool IsBigCity => _city.Size > 20;
		public int CitizenOffset => IsBigCity ? 2 : 8;

		private int _xSum = 0;

		public static int CitizenGroup(Citizen citizen)
		{
			return citizen switch
			{
				Citizen.HappyMale => 0,
				Citizen.HappyFemale => 0,
				Citizen.ContentMale => 1,
				Citizen.ContentFemale => 1,
				Citizen.UnhappyMale => 2,
				Citizen.UnhappyFemale => 2,
				Citizen.Taxman => 3,
				Citizen.Scientist => 3,
				Citizen.Entertainer => 3,
				_ => 3
			};
		}

		public IEnumerable<CitizenDrawInfo> EnumerateCitizens()
		{
			Citizen[] citizens = [.. _city.Citizens];

			int leftStartPackedForBigCities = IsBigCity ? -CitizenOffset + 1 : 0;
			int xx = leftStartPackedForBigCities;
			int group = -1;
			int specialistIndex = -1;

			for (int citizensIndex = 0; citizensIndex < _city.Size; citizensIndex++)
			{
				xx += CitizenOffset;

				if ((int)citizens[citizensIndex] >= 6)
				{
					specialistIndex++;
				}

				if (group != (group = CitizenGroup(citizens[citizensIndex]))
					&& group > 0 && citizensIndex > 0)
				{
					xx += 2;
					if (group == 3) xx += 4;
				}

				yield return new CitizenDrawInfo(citizensIndex, xx, specialistIndex, citizens[citizensIndex]);
			}

			_xSum = xx;
		}

		public int Width()
		{
			return _xSum;
		}
	}
}