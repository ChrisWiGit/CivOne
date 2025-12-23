using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;
using DebugService = System.Diagnostics.Debug;

namespace CivOne.Screens.Services
{
	public interface IGameCitizenDependency :
		IGameUnitsQuery,
		IGameWonderQuery, IGamePlayerQuery,
		IGameTurnQuery, IGameSettings
	{
	}
	public class CityCitizenServiceImpl(
		ICityBasic city,
		ICityBuildings cityBuildings,
		IGameCitizenDependency game,
		List<Citizen> specialists,
		IMap map) : ICityCitizenService
	{
		protected readonly ICityBasic _city = city;
		protected readonly ICityBuildings _cityBuildings = cityBuildings;
		protected readonly IGameCitizenDependency _game = game;
		protected readonly List<Citizen> _specialists = specialists;
		protected readonly IMap _map = map;


		public IEnumerable<CitizenTypes> EnumerateCitizens()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();
			int initialContent = Stage1(ref ct);

			yield return ct;

			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
			// entertainers produce these luxury effects, but also marketplace, bank and luxury trade settings.
			ct = Stage2(ct);

			yield return ct;

			// Stage 3: Building effects
			ct = Stage3(ct);
			yield return ct;

			// Stage 4: martial law
			ct = Stage4(ct, initialContent);
			yield return ct;

			//Stage 5: wonder effects
			ct = Stage5(ct);
				
		}

		public CitizenTypes GetCitizenTypes()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();


			// Stage 1: basic content/unhappy
			// (int initialUnhappyCount, int initialContent) = CalculateCityStats(ct);
			// ct = StageBasic(ct, initialContent, initialUnhappyCount);
			// ApplyEmperorEffects(ct);
			int initialContent = Stage1(ref ct);

			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
			// UpgradeCitizens(ct.Citizens, happyUpgrades);
			ct = Stage2(ct);

			// Stage 3: Building effects
			// ApplyBuildingEffects(ct);
			ct = Stage3(ct);

			// Stage 4: martial law
			// ApplyMartialLaw(ct);
			// ApplyDemocracyEffects(ct, initialContent);
			ct = Stage4(ct, initialContent);

			//Stage 5: wonder effects
			// ApplyWonderEffects(ct);
			// (ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);
			ct = Stage5(ct);

			// In orig Civ citizens change sex if different from previous type
			ct = AdaptCitizens(ct);
			
			return ct;
		}

		Citizen[] ICityCitizenService.GetCitizens()
		{
			return GetCitizenTypes().Citizens;
		}

		protected internal CitizenTypes AdaptCitizens(CitizenTypes ct)
		{
			byte offset = 0;
			for (int i = 1; i < ct.Citizens.Length; i++)
			{
				if (!EqualCitizenType(ct.Citizens[i - 1], ct.Citizens[i]))
				{
					offset = i % 2 == 0 ? (byte)0 : (byte)1;
				}
				ct.Citizens[i] = CitizenByIndex(i + offset, ct.Citizens[i]);
			}
			return ct;
		}

		protected int Stage1(ref CitizenTypes ct)
		{
			(int initialUnhappyCount, int initialContent) =
							CalculateCityStats(ct);

			// Stage 1: basic content/unhappy
			ct = StageBasic(ct, initialContent, initialUnhappyCount);

			ApplyEmperorEffects(ct);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return initialContent;
		}

		protected CitizenTypes Stage2(CitizenTypes ct)
		{
			int lux = _city.Luxuries;
			lux -= _city.EntertainerLuxuries;
			
			int luxuryUpgrades = (int)Math.Floor((double)lux / 2);
			// Luxury goods make content from unhappy citizens
			// and happy from content citizens
			UpgradeCitizens(ct.Citizens, luxuryUpgrades);

			int entertainerUpgrades = (int)Math.Floor((double)_city.EntertainerLuxuries / 2); 
			// Entertainers make happy from content
			UpgradeCitizens(ct.Citizens, entertainerUpgrades);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}

		protected CitizenTypes Stage3(CitizenTypes ct)
		{
			ApplyBuildingEffects(ct);
			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}
		
		protected CitizenTypes Stage4(CitizenTypes ct, int initialContent)
		{
			ApplyMartialLaw(ct);
			ApplyDemocracyEffects(ct, initialContent);
			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}

		protected CitizenTypes Stage5(CitizenTypes ct)
		{
			ApplyWonderEffects(ct);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}

		protected internal (int initialUnhappyCount, int initialContent)
			CalculateCityStats(CitizenTypes ct)
		{
			// max difficulty = 4|5, easiest = 0
			// int difficulty = 4; //Debug only
			int difficulty = _game.Difficulty;
			int specialists = ct.elvis + ct.einstein + ct.taxman;
			int workersAvailable = _city.Size - specialists;

			// https://civfanatics.com/civ1/difficulty/
			// diff 0 = 6 content, all else unhappy
			// diff 1 = 5 content, all else unhappy
			// diff 2 = 4 content, all else unhappy
			// diff 3 = 3 content, all else unhappy
			// diff 4 = 2 content, all else unhappy
			// diff 5 = 1 content, all else unhappy
			// Always 5 as max difficulty for calculation purposes
			// do not use _game.MaxDifficulty here!
			// 6 = max content at easiest level 0
			int contentLimit = 1 + 5 - difficulty;

			// size 4, 0 ent → specialists=0, available=4 → contentLimit=3 → 3c + 1u
			// size 4, 1 ent → specialists=1, available=3 → 2c + 1u + 1ent
			// size 4, 2 ent → specialists=2, available=2 → 1c + 1u + 2ent
			// size 4, 3 ent → specialists=3, available=1 → 0c + 1u + 3ent
			int initialUnhappyCount = Math.Max(0, _city.Size - contentLimit);
			// unhappy will stay the same

			int initialContent = Math.Max(0, workersAvailable - initialUnhappyCount);
			return (initialUnhappyCount, initialContent);
		}

		// This looks like a small trick Sid uses for very large civilizations.
		// At emperor level, you can have 12 cities without problems.
		// After that, natural happiness starts to go away.
		// When you reach 24 cities, each city only has one naturally happy citizen.
		// At 36 cities, there are no naturally happy citizens left.
		// At this point, the Cure for Cancer wonder becomes very useful.
		// When you build more than 36 cities, unhappy (red-shirt) citizens start to appear.
		// Every 12 new cities add one more red-shirt citizen to each city.
		// The good part is they react well to luxury items.
		// Two luxury items can change one from red (very unhappy) to light blue (happy).
		// The bad part is that it’s twice as hard to make them content.
		// A cathedral only makes two of them content.
		protected internal void ApplyEmperorEffects(CitizenTypes ct)
		{
			if (_game.Difficulty < 4)
			{
				return;
			}

			int totalCities = _game.GetPlayer(_city.Owner).Cities.Count();

			if (totalCities <= 12)
			{
				return;
			}

			// >= 24 cities = 1 born-content
			// >= 36 cities = 0 born-content

			int downgradeCount = _city.Size; // case >= 36

			if (totalCities <= 24)
			{
				downgradeCount -= 1;
			}
			DowngradeCitizens(ct.Citizens, downgradeCount);

			WearRedShirt(ct.Citizens, NumberOfRedShirts(totalCities));
		}
		protected internal int NumberOfRedShirts(int totalCities)
		{
			if (totalCities <= 36)
			{
				return 0;
			}
			return 1 + (totalCities - 36) / 12;
			// 1+ (37-36) /12 = 1 + 1/12 = 1
			// 1+ (48-36) /12 = 1 + 12/12 = 2
			// 1+ (61-36) /12 = 1 + 25/12 = 3
		}

		protected internal CitizenTypes CreateCitizenTypes()
		{
			return new()
			{
				happy = 0,
				content = 0,
				unhappy = 0,
				redShirt = 0,
				elvis = _city.Entertainers,
				einstein = _city.Scientists,
				taxman = _city.Taxmen,
				Citizens = new Citizen[_city.Size],
				Buildings = [],
				Wonders = [],
				MarshallLawUnits = []
			};
		}

		protected internal void ApplyWonderEffects(CitizenTypes ct)
		{
			int happy = 0;
			if (_city.Player.HasWonderEffect<HangingGardens>() && !_game.WonderObsolete<HangingGardens>())
			{
				happy += 1;
				ct.Wonders.Add(new HangingGardens());
			}
			if (_city.Player.HasWonderEffect<CureForCancer>() && !_game.WonderObsolete<CureForCancer>())
			{
				happy += 1;
				ct.Wonders.Add(new CureForCancer());
			}

			int contentToHappy = Math.Min(happy, ct.content);
			ContentToHappy(ct.Citizens, contentToHappy);
		}

		protected internal void ApplyDemocracyEffects(CitizenTypes ct, int initialContent)
		{
			if (!_city.Player.RepublicDemocratic)
			{
				return;
			}

			var attackUnitsNotInCity = _game.GetUnits()
				.Where(u => u.IsHome(_city) && u.Attack > 0 && (new Point(u.X, u.Y) != _city.Location));

			ct.MarshallLawUnits.AddRange(attackUnitsNotInCity);

			int unhappyPerUnit = _city.Player.Government is Governments.Republic ? 1 : 2;

			int totalUnhappiness = Math.Min(initialContent, attackUnitsNotInCity.Count() * unhappyPerUnit);

			DowngradeCitizens(ct.Citizens, totalUnhappiness);
		}

		protected internal void ApplyMartialLaw(CitizenTypes ct)
		{
			if (!_city.Player.AnarchyDespotism && !_city.Player.MonarchyCommunist)
			{
				return;
			}

			var attackUnitsInCity = _city.Tile.Units
				.Where(u => u.Attack > 0)
				.OrderByDescending(u => u.Attack); //CW: show strongest units first

			ct.MarshallLawUnits.AddRange(attackUnitsInCity.Take(3));

			const int MAX_MARTIAL_LAW_UNITS = 3;

			int martialLawUnits = Math.Min(MAX_MARTIAL_LAW_UNITS, attackUnitsInCity.Count());
			int unhappyToContent = Math.Min(martialLawUnits, ct.unhappy);

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		protected internal void ApplyBuildingEffects(CitizenTypes ct)
		{
			if (_cityBuildings.HasWonder<ShakespearesTheatre>() && 
				!_game.WonderObsolete<ShakespearesTheatre>())
			{
				// All unhappy become content, but only in this city.
				UnhappyToContent(ct.Citizens, ct.unhappy);

				ct.Wonders.Add(new ShakespearesTheatre());

				// Continuing would not make sense, as all unhappy are already content
				return;
			}

			int unhappyToContent = 0;

			if (_cityBuildings.HasBuilding<Temple>())
			{
				unhappyToContent++;
				if (_city.Player.HasAdvance<Mysticism>()) unhappyToContent <<= 1;
				if (_city.Player.HasWonderEffect<Oracle>())
				{
					unhappyToContent <<= 1;
					ct.Wonders.Add(new Oracle()); 
				}

				ct.Buildings.Add(new Temple());
			}

			if (HasBachsCathedral())
			{
				unhappyToContent += 2;
				ct.Wonders.Add(new JSBachsCathedral());
			}

			if (_cityBuildings.HasBuilding<Colosseum>())
			{
				unhappyToContent += 3;
				ct.Buildings.Add(new Colosseum());
			}

			int cathedralDelta = CathedralDelta();
			if (cathedralDelta > 0)
			{
				ct.Wonders.Add(new MichelangelosChapel());
			}
			unhappyToContent += cathedralDelta;

			if (_cityBuildings.HasBuilding<Cathedral>()) 
			{
				ct.Buildings.Add(new Cathedral());
			}

			if (unhappyToContent <= 0)
			{
				return;
			}

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		internal virtual int CathedralDelta()
		{
			if (!_cityBuildings.HasBuilding<Cathedral>()) return 0;

			int unhappyDelta = 0;

			// CW: Michelangelo's Chapel gives +6 happiness if on same continent as city with wonder, else +4
			// https://civilization.fandom.com/wiki/Michelangelo%27s_Chapel_(Civ1)
			bool isObsolete = _game.WonderObsolete<MichelangelosChapel>();
			bool hasChapelOnSameContinent = !isObsolete &&
						_game.GetPlayer(_city.Owner)
							.CitiesInterface.Any(c => c.HasWonder<MichelangelosChapel>()
					&& c.ContinentId == _city.ContinentId);
			int chapelBonus = !isObsolete && hasChapelOnSameContinent ? 6 : 4;

			unhappyDelta += chapelBonus;

			return unhappyDelta;
		}

		protected virtual internal bool HasBachsCathedral()
		{
			DebugService.Assert(_city.Tile != null, "City has no tile assigned.");
			return _map
					.ContinentCities(_city.ContinentId)
					.Any(city => city.Owner == _city.Owner &&
							city.HasWonder<JSBachsCathedral>());
		}

		protected internal CitizenTypes StageBasic(CitizenTypes ct, int initialContent, int initialUnhappy)
		{
			ct.content = initialContent;
			ct.unhappy = initialUnhappy;

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());

			InitSpecialists(_specialists, ct.Citizens);
			InitCitizens(ct.Citizens, ct.content, ct.unhappy);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}

		protected internal (int happy, int content, int unhappy, int redShirts) CountCitizenTypes(Citizen[] citizens)
		{
			int happy = citizens.Count(c => c is Citizen.HappyMale or Citizen.HappyFemale);
			int content = citizens.Count(c => c is Citizen.ContentMale or Citizen.ContentFemale);
			int unhappy = citizens.Count(c => c is Citizen.UnhappyMale or Citizen.UnhappyFemale);
			int redShirts = citizens.Count(c => c is Citizen.RedShirtMale or Citizen.RedShirtFemale);
			return (happy, content, unhappy, redShirts);
		}


		protected internal void UpgradeCitizens(Citizen[] target, int happyUpgrades)
		{
			if (happyUpgrades <= 0) return;

			var count = target.Length - _specialists.Count;

			// Steps for each citizen, until happyUpgrades run out:
			// 1. unhappy to content if possible then content to happy
			// 2. go to next citizen and repeat 1.
			for (int i = 0; i < count && happyUpgrades > 0; i++)
			{
				// unhappy -> content OR content -> happy
				target[i] = UpgradeCitizen(target[i]);
				happyUpgrades--;

				if (IsHappy(target[i]))
				{
					continue;
				}
				if (happyUpgrades <= 0)
				{
					// CW: Currently, a redshirt is made to unhappy only, not content.
					break;
				}

				// still unhappy because of redshirt?
				if (IsUnhappy(target[i]))
				{
					target[i] = UpgradeCitizen(target[i]);
					happyUpgrades--;
				}

				if (happyUpgrades <= 0)
				{
					break;
				}

				// content -> happy
				target[i] = UpgradeCitizen(target[i]);
				happyUpgrades--;
			}
		}

		protected internal void InitCitizens(Citizen[] target, int content, int unhappy)
		{
			DebugService.Assert(content + unhappy + _specialists.Count == target.Length,
				"Invalid citizen counts for city size.");
			for (int i = 0; i < target.Length - _specialists.Count; i++)
			{
				if (content > 0)
				{
					target[i] = CitizenByIndex(i, Citizen.ContentMale);
					content--;
				}
				else if (unhappy > 0)
				{
					target[i] = CitizenByIndex(i, Citizen.UnhappyMale);
					unhappy--;
				}
			}
		}

		protected internal void InitSpecialists(List<Citizen> specialists, Citizen[] target)
		{
			DebugService.Assert(specialists.Count <= target.Length, "Too many specialists for city size.");
			// Copy specialists to end of array
			Array.Copy(
				sourceArray: specialists.ToArray(),
				sourceIndex: 0,
				destinationArray: target,
				destinationIndex: target.Length - specialists.Count,
				length: specialists.Count);
		}

		protected internal void ContentToHappy(Citizen[] target, int count)
		{
			if (count <= 0) return;

			var total = target.Length - _specialists.Count;

			for (int i = 0; i < total && count > 0; i++)
			{
				if (!IsContent(target[i])) continue;

				target[i] = CitizenByIndex(i, Citizen.HappyMale);
				count--;
			}
		}

		protected internal void UnhappyToContent(Citizen[] target, int count)
		{
			if (count <= 0) return;

			var total = target.Length - _specialists.Count;

			for (int i = 0; i < total && count > 0; i++)
			{
				if (!IsUnhappy(target[i])) continue;

				if (IsRedShirt(target[i]))
				{
					// redshirt takes two steps to become content
					// redshirt -> unhappy -> content
					count--; // first step

					if (count <= 0)
					{
						// CW: currently, we skip upgrading redshirt if not enough count left
						break;
					}
				}

				target[i] = CitizenByIndex(i, Citizen.ContentMale);
				count--; // second step
			}
		}

		protected internal void DowngradeCitizens(Citizen[] target, int count)
		{
			if (count <= 0) return;

			var total = target.Length - _specialists.Count;

			for (int i = 0; i < total && count > 0; i++)
			{
				var downgraded = DowngradeCitizen(target[i]);
				if (downgraded != target[i])
				{
					// happy -> content or content -> unhappy
					target[i] = downgraded;
					count--;
				}
			}
		}

		protected internal void WearRedShirt(Citizen[] target, int count)
		{
			if (count <= 0)
			{
				return;
			}
			var total = target.Length - _specialists.Count;

			for (int i = 0; i < total && i < count; i++)
			{
				target[i] = CitizenByIndex(i, Citizen.RedShirtMale);
			}
		}

		protected internal bool IsContent(Citizen c)
		{
			return c is Citizen.ContentMale or Citizen.ContentFemale;
		}
		
		protected internal bool IsHappy(Citizen c)
		{
			return c is Citizen.HappyMale or Citizen.HappyFemale;
		}

		protected internal bool IsUnhappy(Citizen c)
		{
			return c is Citizen.UnhappyMale or Citizen.UnhappyFemale or Citizen.RedShirtMale or Citizen.RedShirtFemale;
		}

		protected internal bool IsRedShirt(Citizen c)
		{
			return c is Citizen.RedShirtMale or Citizen.RedShirtFemale;
		}


		
		private bool EqualCitizenType(Citizen c1, Citizen c2)
		{
			return c1 switch
			{
				Citizen.HappyMale or Citizen.HappyFemale => c2 is Citizen.HappyMale or Citizen.HappyFemale,
				Citizen.ContentMale or Citizen.ContentFemale => c2 is Citizen.ContentMale or Citizen.ContentFemale,
				Citizen.UnhappyMale or Citizen.UnhappyFemale => c2 is Citizen.UnhappyMale or Citizen.UnhappyFemale,
				Citizen.RedShirtMale or Citizen.RedShirtFemale => c2 is Citizen.RedShirtMale or Citizen.RedShirtFemale,
				_ => false
			};
			// bool isPrevMale = index > 0 && !EqualCitizenType(target[index - 1], target[index]);

		}



		protected internal Citizen CitizenByIndex(int index, Citizen type)
		{
			bool isMale = (index % 2) == 0;
			return type switch
			{
				Citizen.ContentMale or Citizen.ContentFemale => isMale ? Citizen.ContentMale : Citizen.ContentFemale,
				Citizen.HappyMale or Citizen.HappyFemale => isMale ? Citizen.HappyMale : Citizen.HappyFemale,
				Citizen.UnhappyMale or Citizen.UnhappyFemale => isMale ? Citizen.UnhappyMale : Citizen.UnhappyFemale,
				Citizen.Taxman => Citizen.Taxman,
				Citizen.Scientist => Citizen.Scientist,
				Citizen.Entertainer => Citizen.Entertainer,
				Citizen.RedShirtMale or Citizen.RedShirtFemale => isMale ? Citizen.RedShirtMale : Citizen.RedShirtFemale,
				_ => Citizen.ContentMale
			};
		}

		protected internal Citizen DowngradeCitizen(Citizen c)
		{
			return c switch
			{
				Citizen.HappyMale => Citizen.ContentMale,
				Citizen.HappyFemale => Citizen.ContentFemale,
				Citizen.ContentMale => Citizen.UnhappyMale,
				Citizen.ContentFemale => Citizen.UnhappyFemale,
				_ => c
			};
		}

		protected internal Citizen UpgradeCitizen(Citizen c)
		{
			return c switch
			{
				Citizen.RedShirtMale => Citizen.UnhappyFemale,  // CW: Not sure how to upgrade RedShirts, so leave them be
				Citizen.RedShirtFemale => Citizen.UnhappyMale, // they may need 2 upgrades to become content?
				Citizen.UnhappyMale => Citizen.ContentMale,
				Citizen.UnhappyFemale => Citizen.ContentFemale,
				Citizen.ContentMale => Citizen.HappyMale,
				Citizen.ContentFemale => Citizen.HappyFemale,
				_ => c
			};
		}
	}
}