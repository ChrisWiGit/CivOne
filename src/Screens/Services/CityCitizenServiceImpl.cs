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
	public class CityCitizenServiceImpl(
		ICityBasic city,
		ICityBuildings cityBuildings,
		IGame game, List<Citizen> specialists, IMap map) : ICityCitizenService
	{
		protected readonly ICityBasic _city = city;
		protected readonly ICityBuildings _cityBuildings = cityBuildings;
		protected readonly IGame _game = game;
		protected readonly List<Citizen> _specialists = specialists;
		protected readonly IMap _map = map;


		public IEnumerable<CitizenTypes> EnumerateCitizens()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();

			(int specialists, int available, int initialUnhappyCount, int initialContent) = CalculateCityStats(ct, _game);

			// Stage 1: basic content/unhappy
			ct = StageBasic(ct, initialContent, initialUnhappyCount);

			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);
			
			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			
			yield return ct;

			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
			int happyUpgrades = (int)Math.Floor((double)_city.Luxuries / 2);
			UpgradeAllCitizens(ct.Citizens, happyUpgrades);

			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());

			yield return ct;

			// Stage 3: Building effects
			ApplyBuildingEffects(ct);
			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			yield return ct;

			// Stage 4: martial law
			ApplyMartialLaw(ct);
			ApplyDemocracyEffects(ct, available);
			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			yield return ct;

			//Stage 5: wonder effects
			ApplyWonderEffects(ct);
			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			yield return ct;
		}

		public CitizenTypes GetCitizenTypes()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();

			(int specialists, int available, int initialUnhappyCount, int initialContent) = CalculateCityStats(ct, _game);

			// Stage 1: basic content/unhappy
			ct = StageBasic(ct, initialContent, initialUnhappyCount);

			ApplyEmperorEffects(ct);

			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
			int happyUpgrades = (int)Math.Floor((double)_city.Luxuries / 2);
			UpgradeAllCitizens(ct.Citizens, happyUpgrades);

			// Stage 3: Building effects
			ApplyBuildingEffects(ct);

			// Stage 4: martial law
			ApplyMartialLaw(ct);
			ApplyDemocracyEffects(ct, available);

			//Stage 5: wonder effects
			ApplyWonderEffects(ct);
			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			
			return ct;
		}

		protected (int specialists, int available, int initialUnhappyCount, int initialContent)
			CalculateCityStats(CitizenTypes ct, IGame _game)
		{
			// max difficulty = 4|5, easiest = 0
			// int difficulty = 4; //Debug
			int difficulty = _game.Difficulty;
			int specialists = ct.elvis + ct.einstein + ct.taxman;
			int available = _city.Size - specialists;

			// https://civfanatics.com/civ1/difficulty/
			// diff 0 = 6 content, all else unhappy
			// diff 1 = 5 content, all else unhappy
			// diff 2 = 4 content, all else unhappy
			// diff 3 = 3 content, all else unhappy
			// diff 4 = 2 content, all else unhappy
			// diff 5 = 1 content, all else unhappy
			int contentLimit = 6 - difficulty;
			int initialContent = available;

			if (_city.Size > contentLimit)
			{
				initialContent = contentLimit;
			}
			int initialUnhappyCount = Math.Max(0, available - initialContent);


			return (specialists, available, initialUnhappyCount, initialContent);
		}

		// This seems to be a little trick Sid plays on really big civilizations.
		// At emperor level, you can build 12 cities with no ill effects. But then, the born-contents start to disappear. When you have 24 cities, 
		// each city will only have one born-content. By the time you have built 36 cities, there are no born-content citizens anywhere. About now the cure for cancer starts looking real good.
		// When you go beyond 36 cities, you start adding those red-shirted citizens.
		// Each 12 cities you build adds another red shirt in every city.
		// The good news about these guys is they respond well to luxuries. Two diamonds makes one go from red (very unhappy) to light blue (happy). The bad news is it's twice as hard to make them content. A cathedral only makes makes two of them content.
		protected void ApplyEmperorEffects(CitizenTypes ct)
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
				downgradeCount = 1;
			}
			DowngradeCitizens(ct.Citizens, downgradeCount);

			WearRedShirt(ct.Citizens, NumberOfRedShirts(totalCities));
		}
		protected int NumberOfRedShirts(int totalCities)
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

		protected CitizenTypes CreateCitizenTypes()
		{
			return new()
			{
				happy = 0,
				content = 0,
				unhappy = 0,
				redshirt = 0,
				elvis = _city.Entertainers,
				einstein = _city.Scientists,
				taxman = _city.Taxmen,
				Citizens = new Citizen[_city.Size],
				Buildings = [],
				Wonders = [],
				MarshallLawUnits = []
			};
		}

		protected void ApplyWonderEffects(CitizenTypes ct)
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

			int happyToContent = Math.Min(happy, ct.content);
			ContentToHappy(ct.Citizens, happyToContent);
		}

		protected void ApplyDemocracyEffects(CitizenTypes ct, int available)
		{
			if (!_city.Player.RepublicDemocratic)
			{
				return;
			}

			var attackUnitsNotInCity = _game.GetUnits()
				.Where(u => u.Home == _city && u.Attack > 0 && (new Point(u.X, u.Y) != _city.Location));

			ct.MarshallLawUnits.AddRange(attackUnitsNotInCity);

			int unhappyPerUnit = _city.Player.Government is Governments.Republic ? 1 : 2;

			int totalUnhappiness = Math.Min(available, attackUnitsNotInCity.Count() * unhappyPerUnit);

			DowngradeCitizens(ct.Citizens, totalUnhappiness);
		}

		protected void ApplyMartialLaw(CitizenTypes ct)
		{
			if (!_city.Player.AnarchyDespotism && !_city.Player.MonarchyCommunist)
			{
				return;
			}

			var attackUnitsInCity = _city.Tile.Units.Where(u => u.Attack > 0);

			ct.MarshallLawUnits.AddRange(attackUnitsInCity);

			const int MAX_MARTIAL_LAW_UNITS = 3;

			int martialLawUnits = Math.Min(MAX_MARTIAL_LAW_UNITS, attackUnitsInCity.Count());
			int unhappyToContent = Math.Min(martialLawUnits, ct.unhappy);

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		protected void ApplyBuildingEffects(CitizenTypes ct)
		{
			if (_cityBuildings.HasWonder<ShakespearesTheatre>() && !_game.WonderObsolete<ShakespearesTheatre>())
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
					// CW: showing this wonder while processing it in this stage
					// would be confusing for the player to see
					// ct.Wonders.Add(new Oracle()); 
				}

				ct.Buildings.Add(new Temple());
			}

			if (HasBachsCathedral())
			{
				unhappyToContent += 2;
				// CW: Same as above, don't show wonder here
				// ct.Wonders.Add(new JSBachsCathedral());
			}

			if (_cityBuildings.HasBuilding<Colosseum>())
			{
				unhappyToContent += 3;
				ct.Buildings.Add(new Colosseum());
			}

			unhappyToContent += CathedralDelta();

			if (unhappyToContent <= 0)
			{
				return;
			}

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		internal int CathedralDelta()
		{
			if (!_cityBuildings.HasBuilding<Cathedral>()) return 0;

			int unhappyDelta = 0;

			// CW: Michelangelo's Chapel gives +6 happiness if on same continent as city with wonder, else +4
			// https://civilization.fandom.com/wiki/Michelangelo%27s_Chapel_(Civ1)
			bool hasChapel = !_game.WonderObsolete<MichelangelosChapel>()
					&& _game.GetPlayer(_city.Owner).Cities.Any(c => c.HasWonder<MichelangelosChapel>()
					&& c.ContinentId == _city.ContinentId);
			int chapelBonus = hasChapel ? 6 : 4;

			unhappyDelta += chapelBonus;

			return unhappyDelta;
		}

		protected bool HasBachsCathedral()
		{
			return _city.Tile != null
						&& _map.ContinentCities(_city.Tile.ContinentId)
							.Any(x => x.Size > 0 && x.Owner == _city.Owner && x.HasWonder<JSBachsCathedral>());
		}

		protected CitizenTypes StageBasic(CitizenTypes ct, int initialContent, int initialUnhappy)
		{
			ct.content = initialContent;
			ct.unhappy = initialUnhappy;

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());

			InitSpecialists(_specialists, ct.Citizens);
			InitCitizens(ct.Citizens, ct.content, ct.unhappy);

			(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}

		// count citizen types
		protected (int happy, int content, int unhappy) CountCitizenTypes(Citizen[] citizens)
		{
			int happy = citizens.Count(c => c is Citizen.HappyMale or Citizen.HappyFemale);
			int content = citizens.Count(c => c is Citizen.ContentMale or Citizen.ContentFemale);
			int unhappy = citizens.Count(c => c is Citizen.UnhappyMale or Citizen.UnhappyFemale);
			return (happy, content, unhappy);
		}

		protected void UpgradeAllCitizens(Citizen[] target, int happyUpgrades)
		{
			if (happyUpgrades <= 0) return;

			var count = target.Length - _specialists.Count;

			for (int i = 0; i < count && happyUpgrades > 0; i++)
			{
				var upgraded = UpgradeCitizen(target[i]);
				if (upgraded != target[i])
				{
					// unhappy -> content or content -> happy
					target[i] = upgraded;
					happyUpgrades--;

					// still unhappy because of redshirt?
					if (IsUnhappy(target[i]))
					{
						target[i] = UpgradeCitizen(target[i]);
						happyUpgrades--;
					}
				}
				if (target[i] is Citizen.ContentMale or Citizen.ContentFemale)
				{
					// content -> happy
					target[i] = UpgradeCitizen(target[i]);
					happyUpgrades--;
				}
			}
		}

		protected void InitCitizens(Citizen[] target, int content, int unhappy)
		{
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

		protected void InitSpecialists(List<Citizen> specialists, Citizen[] target)
		{
			// Copy specialists to end of array
			Array.Copy(
				sourceArray: specialists.ToArray(),
				sourceIndex: 0,
				destinationArray: target,
				destinationIndex: target.Length - specialists.Count,
				length: specialists.Count);
		}

		protected void ContentToHappy(Citizen[] target, int count)
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

		protected void UnhappyToContent(Citizen[] target, int count)
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
						break;
					}
				}

				target[i] = CitizenByIndex(i, Citizen.ContentMale);
				count--; // second step
			}
		}

		protected void DowngradeCitizens(Citizen[] target, int count)
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

		protected void WearRedShirt(Citizen[] target, int count)
		{
			if (count <= 0) return;

			var total = target.Length - _specialists.Count;

			for (int i = 0; i < total && count > 0; i++)
			{
				target[i] = CitizenByIndex(i, Citizen.RedShirtMale);
			}
		}

		protected bool IsContent(Citizen c)
		{
			return c is Citizen.ContentMale or Citizen.ContentFemale;
		}

		protected bool IsUnhappy(Citizen c)
		{
			return c is Citizen.UnhappyMale or Citizen.UnhappyFemale or Citizen.RedShirtMale or Citizen.RedShirtFemale;
		}

		protected bool IsRedShirt(Citizen c)
		{
			return c is Citizen.RedShirtMale or Citizen.RedShirtFemale;
		}



		// liefert Citizen male, female je nachdem welcher Index
		protected Citizen CitizenByIndex(int index, Citizen type)
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

		protected Citizen DowngradeCitizen(Citizen c)
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

		protected Citizen UpgradeCitizen(Citizen c)
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

		// namespace CivOne.Enums
		// {
		// 	public enum Citizen
		// 	{
		// 		HappyMale = 0,
		// 		HappyFemale = 1,
		// 		ContentMale = 2,
		// 		ContentFemale = 3,
		// 		UnhappyMale = 4,
		// 		UnhappyFemale = 5,
		// 		Taxman = 6,
		// 		Scientist = 7,
		// 		Entertainer = 8,
		// 		RedShirtMale = 9,
		// 		RedShirtFemale = 10
		// 	}
		// }

	}
}

// Beide Methoden in eine Klasse oben zusammenführen. Die machen leicht was anderes, aber das kann man zusammenfassen.
// // Microprose: initial state -> add entertainers -> 'after luxury' state
// // City size 4, King:
// //   3c1u -> 2c1u1ent -> 1h1c1u1ent
// //   3c1u -> 1c1u2ent -> 1h1c2ent
// //   3c1u -> 1u3ent   -> 1h3ent
// // City size 5, King:
// //   3c2u -> 2c2u1ent -> 1h1c2u1ent
// //   3c2u -> 1c2u2ent -> 1h1c1u2ent
// //   3c2u -> 2u3ent   -> 1h1c3ent
// //   3c2u -> 1u4ent   -> 1h4ent
// // City size 6, King:
// //   3c3u -> 2c3u1ent -> 1h1c3u1ent
// //   3c3u -> 1c3u2ent -> 1h1c2u2ent
// //   3c3u -> 3u3ent   -> 1h1c1u3ent
// //   3c3u -> 2u4ent   -> 2h4ent

// internal IEnumerable<CitizenTypes> Residents
// {
// 	get
// 	{
// 		// TODO fire-eggs: add side-effect of recalc specialties a la Citizens
// 		CitizenTypes start = new()
// 		{
// 			elvis = Entertainers,
// 			einstein = Scientists,
// 			taxman = Taxmen,
// 			Wonders = [],
// 			Buildings = [],
// 			MarshallLawUnits = []
// 		};

// 		int specialists = start.elvis + start.einstein + start.taxman;
// 		int available = Size - specialists;
// 		int initialContent = Game.MaxDifficulty - Game.Difficulty;

// 		// Stage 1: basic content/unhappy
// 		start.content = Math.Max(0, Math.Min(available, initialContent - specialists));
// 		start.unhappy = available - start.content;

// 		Debug.Assert(start.Sum() == Size);
// 		Debug.Assert(start.Valid());
// 		yield return start;

// 		if (available < 1)
// 			yield return start;
// 		else
// 		{
// 			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
// 			int happyUpgrades = (int)Math.Floor((double)Luxuries / 2);
// 			int cont = start.content;
// 			int unha = start.unhappy;
// 			int happ = start.happy;
// 			for (int h = 0; h < happyUpgrades; h++)
// 			{
// 				if (cont > 0)
// 				{
// 					happ++;
// 					cont--;
// 					continue;
// 				}
// 				if (unha > 0)
// 				{
// 					cont++;
// 					unha--;
// 				}
// 			}

// 			start.happy = happ;
// 			start.content = cont;
// 			start.unhappy = unha;

// 			Debug.Assert(start.Sum() == Size);
// 			Debug.Assert(start.Valid());

// 			// TODO fire-eggs impact of luxury setting?
// 			yield return start;
// 		}

// 		// Stage 3: Building effects
// 		int unhappyDelta = 0;
// 		if (HasBuilding<Temple>())
// 		{
// 			int templeEffect = 1;
// 			if (Player.HasAdvance<Mysticism>()) templeEffect <<= 1;
// 			if (Player.HasWonderEffect<Oracle>()) templeEffect <<= 1;
// 			unhappyDelta += templeEffect;
// 			start.Buildings.Add(new Temple());
// 		}

// 		int delta = CathedralDelta(); ;
// 		unhappyDelta += delta;
// 		if (delta > 0)
// 		{
// 			start.Buildings.Add(new Cathedral());
// 		}
// 		if (HasBuilding<Colosseum>())
// 		{
// 			unhappyDelta += 3;
// 			start.Buildings.Add(new Colosseum());
// 		}

// 		// Stage 3: Building effects
// 		unhappyDelta = Math.Min(start.unhappy, unhappyDelta);
// 		start.content += unhappyDelta;
// 		start.unhappy -= unhappyDelta;
// 		yield return start;

// 		unhappyDelta = 0;

// 		// 						In the original Sid Meier's Civilization, martial law is a 
// 		// mechanism of quelling citizens' discontent available under Anarchy, 
// 		// Despotism, Monarchy or Communism. Martial law allows the ruler to turn unhappy citizens content by stationing up to 3 military units inside the city. Each unit makes 1 citizen content, but no more than 3 in total (additional units will have no effect on the population's mood).

// 		// Units eligible to impose martial law are any units with
// 		// an attack of 1 or more including ships and air units.

// 		// Note that in the first MS-DOS version of Civilization (version 474.01/475.01) the 
// 		//limit of 3 units is not present, and any number of unhappy citizens can be quelled by enough military units.

// 		// Stage 4: martial law
// 		// if (Player.AnarchyDespotism || Player.MonarchyCommunist)
// 		// {
// 		// 	var attackUnitsInCity = Game.Instance.GetUnits()
// 		// 		.Where(u => u.X == this.X && u.Y == this.Y && u.Attack > 0);

// 		// 	start.Units = [.. attackUnitsInCity];

// 		// 	// CW: not as in original Civ, limit to max 3 units
// 		// 	const int MAX_MARTIAL_LAW_UNITS = 3;

// 		// 	unhappyDelta += Math.Max(MAX_MARTIAL_LAW_UNITS, attackUnitsInCity.Count());
// 		// }

// 		// Every unit outside its home city causes 2 unhappiness. (exceptions: settlers, diplomats, caravans, transports).
// 		// Every unit outside their home city causes 1 unhappiness. (exceptions: settlers, transports, diplomats, caravans)
// 		// if (Player.RepublicDemocratic)
// 		{
// 			var attackUnitsNotInCity = Game.Instance.GetUnits()
// 				.Where(u => u.Home == this && u.Attack > 0 && (u.X != this.X || u.Y != this.Y));

// 			start.MarshallLawUnits = [.. attackUnitsNotInCity];

// 			int unhappy = Player.Government is Republic ? 1 : 2;

// 			start.unhappy += Math.Min(Size, attackUnitsNotInCity.Count() * unhappy);
// 			start.content = Math.Max(0, start.content - attackUnitsNotInCity.Count() * unhappy);
// 		}
// 		yield return start;

// 		//Stage 5: wonder effects

// 		if (HasWonder<ShakespearesTheatre>() && !Game.WonderObsolete<ShakespearesTheatre>())
// 		{
// 			// All unhappy become content, but only in this city.
// 			unhappyDelta = start.unhappy;
// 			start.Wonders.Add(new ShakespearesTheatre());
// 		}
// 		int happy = 0;
// 		if (Player.HasWonderEffect<HangingGardens>())
// 		{
// 			happy += 1;
// 			start.Wonders.Add(new HangingGardens());
// 		}
// 		if (Player.HasWonderEffect<CureForCancer>())
// 		{
// 			happy += 1;
// 			start.Wonders.Add(new CureForCancer());
// 		}

// 		unhappyDelta = Math.Min(start.unhappy, unhappyDelta);
// 		start.content += unhappyDelta;
// 		start.unhappy -= unhappyDelta;
// 		start.happy += Math.Min(happy, start.content);
// 		start.content -= Math.Min(happy, start.content);

// 		Debug.Assert(start.Sum() == Size);
// 		Debug.Assert(start.Valid());

// 		yield return start;
// 	}
// }

// internal int CathedralDelta()
// {
// 	if (!HasBuilding<Cathedral>()) return 0;

// 	int unhappyDelta = 0;

// 	// CW: Michelangelo's Chapel gives +6 happiness if on same continent as city with wonder, else +4
// 	// https://civilization.fandom.com/wiki/Michelangelo%27s_Chapel_(Civ1)
// 	bool hasChapel = !Game.WonderObsolete<MichelangelosChapel>()
// 			&& Game.GetPlayer(_owner).Cities.Any(c => c.HasWonder<MichelangelosChapel>() && c.ContinentId == ContinentId);
// 	int chapelBonus = hasChapel ? 6 : 4;

// 	unhappyDelta += chapelBonus;

// 	return unhappyDelta;
// }


// internal IEnumerable<Citizen> Citizens
// {
// 	get
// 	{
// 		// Update specialist count
// 		while (_specialists.Count < Size - (ResourceTiles.Count() - 1)) _specialists.Add(Citizen.Entertainer);
// 		while (_specialists.Count > Size - (ResourceTiles.Count() - 1)) _specialists.Remove(_specialists.Last());

// 		// TODO fire-eggs verify luxury makes happy first, then clears unhappy
// 		int happyCount = (int)Math.Floor((double)Luxuries / 2);
// 		if (Player.HasWonderEffect<HangingGardens>()) happyCount++;
// 		if (Player.HasWonderEffect<CureForCancer>()) happyCount++;

// 		int unhappyCount = Size - (Game.MaxDifficulty - Game.Difficulty) - happyCount;
// 		if (HasWonder<ShakespearesTheatre>() && !Game.WonderObsolete<ShakespearesTheatre>())
// 		{
// 			// All unhappy become content, but only in this city.
// 			unhappyCount = 0;
// 		}
// 		else
// 		{
// 			if (HasBuilding<Temple>())
// 			{
// 				int templeEffect = 1;
// 				if (Player.HasAdvance<Mysticism>()) templeEffect <<= 1;
// 				if (Player.HasWonderEffect<Oracle>()) templeEffect <<= 1;
// 				unhappyCount -= templeEffect;
// 			}
// 			if (Tile != null && Map.ContentCities(Tile.ContinentId).Any(x => x.Size > 0 && x.Owner == Owner && x.HasWonder<JSBachsCathedral>()))
// 			{
// 				unhappyCount -= 2;
// 			}
// 			if (HasBuilding<Colosseum>()) unhappyCount -= 3;
// 			unhappyCount -= CathedralDelta();
// 		}

// 		// 20190612 fire-eggs Martial law : reduce unhappy count for every attack-capable unit in city [max 3]
// 		if (Player.AnarchyDespotism || Player.MonarchyCommunist)
// 		{
// 			var attackUnitsInCity = Game.Instance.GetUnits()
// 				.Where(u => u.X == this.X && u.Y == this.Y && u.Attack > 0)
// 				.Count();
// 			attackUnitsInCity = Math.Min(attackUnitsInCity, 3);
// 			unhappyCount -= attackUnitsInCity;

// 			// TODO fire-eggs: absent units make people unhappy (republic, democracy)
// 		}

// 		int content = 0;
// 		int unhappy = 0;
// 		int working = (ResourceTiles.Count() - 1);
// 		int specialist = 0;

// 		for (int i = 0; i < Size; i++)
// 		{
// 			if (i < working)
// 			{
// 				if (happyCount-- > 0)
// 				{
// 					yield return (i % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale;
// 					continue;
// 				}
// 				if ((unhappyCount - (working - i)) >= 0)
// 				{
// 					unhappyCount--;
// 					yield return ((unhappy++) % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale;
// 					continue;
// 				}
// 				yield return ((content++) % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale;
// 				continue;
// 			}
// 			yield return _specialists[specialist++];
// 		}
// 	}
// }
// internal void ChangeSpecialist(int index)
// {
// 	if (index >= _specialists.Count) return;

// 	while (_specialists.Count < (index + 1)) _specialists.Add(Citizen.Entertainer);
// 	_specialists[index] = (Citizen)((((int)_specialists[index] - 5) % 3) + 6);
// }