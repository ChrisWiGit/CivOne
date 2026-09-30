using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		IGameTurnQuery, IGameSettings, IGameCityQuery
	{
	}

	/// <summary>
	/// Calculates city happiness.
	///
	/// Unhappiness that does not fit into the city is parked outside it and flows back whenever a modifier
	/// lowers the visible unhappiness again, so a large empire needs more temples, colosseums and luxuries
	/// than the visible unhappy count suggests. The base unhappiness carries an empire size penalty that grows
	/// with the number of cities and with the government.
	/// </summary>
	[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The list is intended for internal use and is not exposed as a public API.")]
	#pragma warning disable CA1822 // Mark members as static
	public class CityCitizenService : ICityCitizenService
	{
		readonly ICityBasic _city;
		readonly ICityBuildings _cityBuildings;
		readonly IGameCitizenDependency _game;
		readonly List<Citizen> _specialists;
		readonly IMap _map;
		readonly PendingUnhappinessRefill _refill;
		readonly int? _luxuryRate;

		/// <summary>
		/// Unhappiness that does not fit into the city.
		/// Reset at the start of every calculation, see <see cref="Stage1"/>.
		/// </summary>
		int _pendingUnhappiness;

		public CityCitizenService(
			ICityBasic city,
			ICityBuildings cityBuildings,
			IGameCitizenDependency game,
			List<Citizen> specialists,
			IMap map,
			int? luxuryRate = null) : this(city, cityBuildings, game, specialists, map, refill: null, luxuryRate)
		{
		}

		internal CityCitizenService(
			ICityBasic city,
			ICityBuildings cityBuildings,
			IGameCitizenDependency game,
			List<Citizen> specialists,
			IMap map,
			PendingUnhappinessRefill? refill,
			int? luxuryRate = null)
		{
			_city = city;
			_cityBuildings = cityBuildings;
			_game = game;
			_specialists = specialists;
			_map = map;
			_refill = refill ?? new EqualisingPendingUnhappinessDelegate().Refill;
			_luxuryRate = luxuryRate;
		}

		/// <summary>
		/// Gets the city the citizens are calculated for.
		/// </summary>
		protected ICityBasic City => _city;

		/// <summary>
		/// Gets the buildings and wonders of <see cref="City"/>.
		/// </summary>
		protected ICityBuildings CityBuildings => _cityBuildings;

		/// <summary>
		/// Gets the game state the calculation depends on.
		/// </summary>
		protected IGameCitizenDependency GameState => _game;

		/// <summary>
		/// Gets the specialists of <see cref="City"/>.
		/// These occupy the last entries of the citizen array and are never changed by the calculation.
		/// </summary>
		protected int SpecialistCount => _specialists.Count;

		/// <summary>
		/// Gets the share of trade that becomes luxuries, in tenths.
		/// Without a rate from the caller this is the rate the player has set, which is whatever the tax and
		/// science sliders leave over.
		/// </summary>
		internal int LuxuryRate => _luxuryRate
			?? Math.Clamp(10 - _city.PlayerIntf.ScienceRate - _city.PlayerIntf.TaxesRate, 0, 10);

		/// <summary>
		/// Gets the luxuries of the city, in the order the original applies them.
		///
		/// Corruption is taken off the trade before the luxury share, but the result may never exceed the
		/// trade the city really has. Entertainers are added before the market place and the bank, so their
		/// luxuries are multiplied by those buildings as well.
		/// </summary>
		/// <returns>The luxury points the city produces.</returns>
		internal int Luxuries()
		{
			int trade = Math.Max(_city.TradeTotalGross, 0);
			int corruption = Math.Clamp(_city.LuxuryCorruption, 0, trade);

			// The 5 sits inside the numerator, so the product is rounded, not the quotient.
			int luxuries = Math.Clamp(((LuxuryRate * (trade - corruption)) + 5) / 10, 0, trade);

			luxuries += _city.Entertainers * 2;

			if (_cityBuildings.HasBuilding<MarketPlace>())
			{
				luxuries += luxuries / 2;
			}

			if (_cityBuildings.HasBuilding<Bank>())
			{
				luxuries += luxuries / 2;
			}

			return luxuries;
		}

		/// <summary>
		/// Highest difficulty the empire size penalty is defined for.
		///
		/// The original knows five levels, chieftain up to emperor. Deity is an addition of this project,
		/// and continuing the formula into it would start the penalty at five cities under a despotism,
		/// steeper than anything the original can produce. Deity therefore shares the emperor value.
		/// </summary>
		internal const int MaxPenaltyDifficulty = 4;

		/// <summary>
		/// Gets the empire size at which unhappiness starts to grow.
		/// The value depends on the difficulty and on the government: the more advanced the government, the
		/// more cities an empire carries before the penalty starts.
		/// </summary>
		/// <returns>The city count the penalty starts above, or 0 when the difficulty leaves no room.</returns>
		internal int EmpireSizeBase()
		{
			int difficulty = Math.Clamp(_game.Difficulty, 0, MaxPenaltyDifficulty);
			int governmentId = _city.PlayerIntf.Government.Id;

			// The original halves 14 - 2 x difficulty, which is 7 - difficulty, and scales it by the
			// government: anarchy and despotism 2, monarchy and communism 3, republic and democracy 4.
			return Math.Max(((governmentId / 2) + 2) * (7 - difficulty), 0);
		}

		/// <summary>
		/// Gets the extra unhappiness this city carries because the empire is large.
		/// The penalty is distributed by city slot, so it reaches one city per <see cref="EmpireSizeBase"/>
		/// cities at a time rather than all cities at once.
		/// </summary>
		/// <returns>The number of extra unhappy citizens, never negative.</returns>
		internal int EmpireSizePenalty()
		{
			int sizeBase = EmpireSizeBase();
			if (sizeBase <= 0)
			{
				return 0;
			}

			int cityCount = _game.GetPlayer(_city.CityOwnerPlayerIndex)?.Cities.Length ?? 0;

			// A city that is not part of the game has no slot; treat it as the first one.
			int cityIndex = Math.Max(_game.GetCityIndex(_city), 0);

			return Math.Max(((cityIndex % sizeBase) + cityCount - sizeBase) / sizeBase, 0);
		}

		/// <summary>
		/// Gets the unhappiness the city starts with, before any modifier.
		/// The value is deliberately not clamped here, because the empire size penalty has to be added
		/// before the lower clamp: a small city in a large empire is unhappy even though its size alone
		/// would not make it so.
		/// </summary>
		/// <returns>The raw base unhappiness, which may be negative.</returns>
		internal int BaseUnhappy()
		{
			if (!_game.IsHumanPlayer(_city.CityOwnerPlayerIndex))
			{
				// The computer players carry neither the difficulty scaling nor the empire size penalty.
				return _city.Size - 3;
			}

			return _city.Size + _game.Difficulty - 6 + EmpireSizePenalty();
		}

		public IEnumerable<CitizenTypes> EnumerateCitizens()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();
			int initialContent = Stage1(ref ct);

			yield return ct;

			// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
			// entertainers produce these luxury effects, but also marketplace, bank and luxury trade settings.
			ct = Stage2(ct);
			// In orig Civ citizens change sex if different from previous type
			ct = AdaptCitizens(ct);

			yield return ct;

			// Stage 3: Building effects
			ct = Stage3(ct);
			ct = AdaptCitizens(ct);
			yield return ct;

			// Stage 4: martial law or war weariness
			ct = Stage4(ct, initialContent);
			ct = AdaptCitizens(ct);
			yield return ct;

			//Stage 5: wonder effects
			ct = Stage5(ct);
			ct = AdaptCitizens(ct);
			yield return ct;
		}

		public CitizenTypes GetCitizenTypes()
		{
			DebugService.Assert(_specialists.Count <= _city.Size);
			CitizenTypes ct = CreateCitizenTypes();

			// Stage 1: base unhappiness and the empire size penalty
			int initialContent = Stage1(ref ct);

			// Stage 2: impact of luxuries
			ct = Stage2(ct);

			// Stage 3: Building effects
			ct = Stage3(ct);

			// Stage 4: martial law or war weariness
			ct = Stage4(ct, initialContent);

			//Stage 5: wonder effects
			ct = Stage5(ct);

			// In orig Civ citizens change sex if different from previous type
			ct = AdaptCitizens(ct);

			return ct;
		}

		[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "The interface is only intended to be used via the static Create method and not directly implemented by external classes.")]
		Citizen[] ICityCitizenService.GetCitizens()
		{
			return GetCitizenTypes().Citizens;
		}

		/// <summary>
		/// Adapt citizens.
		/// This method makes sure that the first citizen of each type
		/// is a male, the second a female, and so on.
		/// This is how the original Civ seems to do it.
		/// (imo, a rather weird way of doing it, because of sex changes on upgrades/downgrades)
		/// </summary>
		/// <param name="ct">The citizen types.</param>
		/// <returns>>The adapted citizen types on the same instance.</returns>
		protected internal CitizenTypes AdaptCitizens(CitizenTypes ct)
		{
			if (ct.Citizens.Length < 2)
			{
				return ct;
			}

			int indexWithinType = 0;

			ct.Citizens[0] = CitizenByIndex(0, ct.Citizens[0]);

			for (int i = 1; i < ct.Citizens.Length; i++)
			{
				if (!EqualCitizenType(ct.Citizens[i - 1], ct.Citizens[i]))
				{
					indexWithinType = 0;
				}
				else
				{
					indexWithinType++;
				}

				ct.Citizens[i] = CitizenByIndex(indexWithinType, ct.Citizens[i]);
			}

			return ct;
		}

		/// <summary>
		/// Sets the base content and unhappy counts and parks everything that does not fit into the city.
		///
		/// This is also the place the parked unhappiness is reset, because both entry points of the service
		/// start here. Without the reset a second call on the same instance would inherit the surplus of the
		/// first one.
		/// </summary>
		/// <param name="ct">The citizen types to fill.</param>
		/// <returns>The initial content count, which stage 4 uses as the cap for war weariness.</returns>
		protected internal int Stage1(ref CitizenTypes ct)
		{
			_pendingUnhappiness = 0;

			int specialistCount = ct.elvis + ct.einstein + ct.taxman;
			int workersAvailable = Math.Max(_city.Size - specialistCount, 0);

			int unhappyCount = Math.Max(BaseUnhappy(), 0);

			// Only what does not fit into the city itself is parked. The seats the specialists take up are
			// not part of that test: the original parks the empire size penalty that exceeds the city size,
			// and lets the normalisation squeeze the rest into the seats afterwards. Parking against the
			// seats instead would leave a surplus behind that shields the happy citizens from the very first
			// normalisation, which raises the happy count of every crowded city by one.
			_pendingUnhappiness = Math.Max(unhappyCount - _city.Size, 0);

			int initialUnhappy = Math.Min(workersAvailable, unhappyCount);
			int initialContent = Math.Max(0, workersAvailable - unhappyCount);

			ct = StageBasic(ct, initialContent, initialUnhappy);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			// The raw count goes into the normalisation, not the one the citizen array could hold, so the
			// squeeze into the seats happens there and spends the parked unhappiness in the right order.
			ct = Normalise(ct, ct.happy, unhappyCount, markPending: false);
			return initialContent;
		}

		/// <summary>
		/// Turns luxuries into happy citizens.
		/// The original sets the happy count outright instead of upgrading citizens one by one, and lets the
		/// normalisation push the unhappy count down to make room. That is how luxuries end a disorder.
		/// </summary>
		/// <param name="ct">The citizen types of the previous stage.</param>
		/// <returns>The citizen types after the luxuries.</returns>
		protected internal CitizenTypes Stage2(CitizenTypes ct)
		{
			int happy = Luxuries() / 2;
			return Normalise(ct, happy, ct.unhappy + ct.redShirt, markPending: false);
		}

		protected internal CitizenTypes Stage3(CitizenTypes ct)
		{
			ApplyBuildingEffects(ct);
			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return Normalise(ct, markPending: false);
		}

		/// <summary>
		/// Applies martial law or war weariness, never both.
		/// The original picks one by government: everything below a republic keeps order with troops, while
		/// a republic and a democracy grow weary of the troops that are away instead.
		/// </summary>
		/// <param name="ct">The citizen types of the previous stage.</param>
		/// <param name="initialContent">
		/// The content count stage 1 started with.
		/// Martial law does not need it, and war weariness is uncapped in the original, so this stage ignores it.
		/// </param>
		/// <returns>The citizen types after the stage.</returns>
		protected internal CitizenTypes Stage4(CitizenTypes ct, int initialContent)
		{
			if (!_city.PlayerIntf.RepublicDemocratic)
			{
				ApplyMartialLaw(ct);
				(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

				return Normalise(ct, markPending: false);
			}

			// War weariness raises a counter in the original rather than downgrading single citizens, and it
			// has no upper bound of its own. The normalisation behind it is what limits the result, exactly as
			// in stage 2. Going through DowngradeCitizens here would cap the effect at the citizens that happen
			// to be downgradable, which the original does not do.
			int weariness = WarWeariness(ct);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			return Normalise(ct, ct.happy, ct.unhappy + ct.redShirt + weariness, markPending: false);
		}

		/// <summary>
		/// Applies the wonders of stage 5, in the order the original uses: the Hanging Gardens and the Cure
		/// For Cancer make a citizen happy each, then Shakespeare's Theatre clears the unhappiness of its own
		/// city, then J.S. Bach's Cathedral takes two more away across the continent.
		///
		/// Placing Shakespeare here rather than with the buildings matters: stage 4 sits in between, so the
		/// theatre wipes out the war weariness of its city, while applying it two stages earlier would let the
		/// weariness make citizens unhappy again afterwards.
		/// </summary>
		/// <param name="ct">The citizen types to change.</param>
		/// <returns>The citizen types after the stage.</returns>
		protected internal CitizenTypes Stage5(CitizenTypes ct)
		{
			ApplyWonderEffects(ct);

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return Normalise(ct, markPending: true);
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

		/// <summary>
		/// Applies the wonders: the Hanging Gardens and the Cure For Cancer make a citizen happy each,
		/// then Shakespeare's Theatre clears the unhappiness of its own city, then J.S. Bach's Cathedral
		/// takes two more away across the continent.
		/// </summary>
		/// <param name="ct">The citizen types to change.</param>
		protected internal void ApplyWonderEffects(CitizenTypes ct)
		{
			int happy = 0;
			if (_city.PlayerIntf.HasWonderEffect<HangingGardens>() && !_game.WonderObsolete<HangingGardens>())
			{
				happy += 1;
				ct.Wonders.Add(new HangingGardens());
			}
			if (_city.PlayerIntf.HasWonderEffect<CureForCancer>() && !_game.WonderObsolete<CureForCancer>())
			{
				// CW: check for obsoletion first,
				// even if in original Civ it is not done.
				// But if we extend the game later, this may become relevant.
				happy += 1;
				ct.Wonders.Add(new CureForCancer());
			}

			int contentToHappy = Math.Min(happy, ct.content);
			ContentToHappy(ct.Citizens, contentToHappy);

			(_, _, int unhappy, int redShirt) = CountCitizenTypes(ct.Citizens);

			if (_cityBuildings.HasWonder<ShakespearesTheatre>() &&
				!_game.WonderObsolete<ShakespearesTheatre>())
			{
				// All unhappy become content, but only in this city.
				UnhappyToContent(ct.Citizens, unhappy + redShirt);

				ct.Wonders.Add(new ShakespearesTheatre());
			}

			if (HasBachsCathedral())
			{
				UnhappyToContent(ct.Citizens, BachsCathedralUnhappyToContent);
				ct.Wonders.Add(new JSBachsCathedral());
			}
		}

		/// <summary>
		/// How much unhappiness J.S. Bach's Cathedral takes away from every city of its owner that stands on
		/// the same continent as the city holding it.
		/// </summary>
		private const int BachsCathedralUnhappyToContent = 2;

		/// <summary>
		/// Counts how weary the citizens are of the troops that are away from home.
		///
		/// A republic loses one citizen per unit abroad and a democracy two. Women's Suffrage removes one of
		/// those points, so a democracy that has it is as weary as a republic without it, and a republic that
		/// has it is not weary at all. The wonder never adds unhappiness.
		///
		/// A unit counts when it can attack, is supported by this city, and is either an air unit or standing
		/// somewhere other than the city itself. Air units count even while they sit at home; ships and land
		/// units do not. Settlers, diplomats, caravans and transports have no attack value and never count.
		/// </summary>
		/// <param name="ct">The citizen types, whose unit list is filled for the city screen.</param>
		/// <returns>The unhappiness the troops abroad cause, before normalisation.</returns>
		private int WarWeariness(CitizenTypes ct)
		{
			IUnit[] unitsAway = [.. _game.GetUnits()
				.Where(u => u.IsHome(_city) && u.Attack > 0
					&& (u.UnitCategory == UnitClass.Air || new Point(u.X, u.Y) != _city.Location))];

			ct.MarshallLawUnits.AddRange(unitsAway);

			int unhappyPerUnit = _city.PlayerIntf.HasWonderEffect<WomensSuffrage>() ? 0 : 1;
			if (_city.PlayerIntf.Government is Governments.Democracy)
			{
				unhappyPerUnit++;
			}

			return unitsAway.Length * unhappyPerUnit;
		}

		protected internal void ApplyMartialLaw(CitizenTypes ct)
		{
			if (!_city.PlayerIntf.AnarchyDespotism && !_city.PlayerIntf.MonarchyCommunist)
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

		/// <summary>
		/// Applies the buildings: the colosseum, the cathedral and the temple with the oracle.
		/// </summary>
		/// <param name="ct">The citizen types to change.</param>
		protected internal void ApplyBuildingEffects(CitizenTypes ct)
		{
			int unhappyToContent = 0;

			if (_cityBuildings.HasBuilding<Temple>())
			{
				// The temple is gated the way the cathedral is, only with two steps instead of one: Mysticism
				// makes it worth two, Ceremonial Burial alone one, and without either it has no effect.
				// All three branches are reachable. Ceremonial Burial is the temple's build prerequisite, so
				// the empty branch belongs to a captured city, whose temple stays idle until its new owner
				// researches the advance. Mysticism requires Ceremonial Burial, so "Mysticism without
				// Ceremonial Burial" is the one combination that cannot occur.
				bool hasMysticism = _city.PlayerIntf.HasAdvance<Mysticism>();

				if (hasMysticism)
				{
					unhappyToContent += 2;
				}
				else if (_city.PlayerIntf.HasAdvance<CeremonialBurial>())
				{
					unhappyToContent += 1;
				}

				// The oracle sits inside the temple block, so it needs a temple but no advance of its own.
				// It follows the temple's Mysticism step without inheriting the Ceremonial Burial gate.
				if (_city.PlayerIntf.HasWonderEffect<Oracle>())
				{
					unhappyToContent += hasMysticism ? 2 : 1;
					ct.Wonders.Add(new Oracle());
				}

				ct.Buildings.Add(new Temple());
			}

			if (_cityBuildings.HasBuilding<Colosseum>())
			{
				unhappyToContent += 3;
				ct.Buildings.Add(new Colosseum());
			}

			int cathedralDelta = CathedralDelta();
			if (cathedralDelta > 0 && HasMichelangelosChapelEffect())
			{
				ct.Wonders.Add(new MichelangelosChapel());
			}
			unhappyToContent += cathedralDelta;

			if (_cityBuildings.HasBuilding<Cathedral>())
			{
				ct.Buildings.Add(new Cathedral());
			}

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		/// <summary>
		/// Gets how much unhappiness a cathedral takes away.
		///
		/// The whole block sits behind a check on the advance Religion. Religion is also what allows a
		/// cathedral to be built, so the check decides one case only, and it is a real one: a captured city
		/// brings the building without bringing the advance. Such a cathedral is idle until its new owner
		/// researches Religion, and then it works retroactively.
		///
		/// Michelangelo's Chapel raises the effect for every city of its owner, wherever those cities stand:
		/// the chapel is not bound to a continent, only J.S. Bach's Cathedral is.
		/// </summary>
		/// <returns>The unhappiness the cathedral removes, or 0 when there is none.</returns>
		internal virtual int CathedralDelta()
		{
			if (!_cityBuildings.HasBuilding<Cathedral>())
			{
				return 0;
			}

			if (!_city.PlayerIntf.HasAdvance<Religion>())
			{
				return 0;
			}

			return HasMichelangelosChapelEffect() ? 6 : 4;
		}

		/// <summary>
		/// Tells whether Michelangelo's Chapel helps this city.
		/// The chapel is not bound to a continent, so owning its effect is enough.
		/// </summary>
		/// <returns><c>true</c> when the owner has the chapel's effect.</returns>
		internal virtual bool HasMichelangelosChapelEffect() => _city.PlayerIntf.HasWonderEffect<MichelangelosChapel>();

		protected virtual internal bool HasBachsCathedral()
		{
			DebugService.Assert(_city.Tile != null, "City has no tile assigned.");
			return _map
					.ContinentCities(_city.ContinentId)
					.Any(city => city.CityOwnerPlayerIndex == _city.CityOwnerPlayerIndex &&
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

		[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The list is intended for internal use and is not exposed as a public API.")]
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

		/// <summary>
		/// Lets the parked unhappiness flow back, then brings the happy and unhappy counts back into the
		/// seats the city has. Runs after every stage, so a modifier that overshoots loses its surplus at
		/// its own stage instead of carrying it into the next one.
		/// </summary>
		/// <param name="ct">The citizen types of the finished stage.</param>
		/// <param name="markPending">
		/// Whether the citizens standing for the parked unhappiness are drawn as red shirts.
		/// Only the last stage does this, so the extra cost of a red shirt is not charged once per stage.
		/// </param>
		/// <returns>The corrected citizen types.</returns>
		private CitizenTypes Normalise(CitizenTypes ct, bool markPending)
		{
			return Normalise(ct, ct.happy, ct.unhappy + ct.redShirt, markPending);
		}

		/// <summary>
		/// Normalises the given counts instead of the ones the citizen array currently holds.
		/// Stage 2 needs this, because the happy count is set from the luxuries rather than
		/// derived from the citizens.
		/// </summary>
		/// <param name="ct">The citizen types of the finished stage.</param>
		/// <param name="happy">The happy count to normalise.</param>
		/// <param name="unhappy">The unhappy count to normalise.</param>
		/// <param name="markPending">Whether the pending unhappiness is drawn as red shirts.</param>
		/// <returns>The corrected citizen types.</returns>
		private CitizenTypes Normalise(CitizenTypes ct, int happy, int unhappy, bool markPending)
		{
			int seats = Math.Max(_city.Size - SpecialistCount, 0);

			int pending = _pendingUnhappiness;

			_refill(ref unhappy, ref pending);

			happy = Math.Clamp(happy, 0, _city.Size);
			unhappy = Math.Clamp(unhappy, 0, _city.Size);

			while (happy + unhappy > seats)
			{
				if (pending > 0 && unhappy > 0)
				{
					pending--;
				}
				else
				{
					happy = Math.Max(happy - 1, 0);
				}

				unhappy = Math.Max(unhappy - 1, 0);
			}

			_pendingUnhappiness = pending;

			return WriteCitizens(ct, happy, unhappy, markPending);
		}

		/// <summary>
		/// Writes the corrected counts back into the citizen array.
		/// Happy citizens come first, then content ones, then unhappy ones. The specialists keep the tail of
		/// the array and are never touched.
		/// </summary>
		/// <param name="ct">The citizen types to write into.</param>
		/// <param name="happy">The happy citizen count.</param>
		/// <param name="unhappy">The unhappy citizen count.</param>
		/// <param name="markPending">Whether the parked unhappiness is drawn as red shirts.</param>
		/// <returns>The citizen types with the counts recounted from the array.</returns>
		private CitizenTypes WriteCitizens(CitizenTypes ct, int happy, int unhappy, bool markPending)
		{
			int seats = Math.Max(_city.Size - SpecialistCount, 0);
			int content = seats - happy - unhappy;

			DebugService.Assert(content >= 0, "More happy and unhappy citizens than the city has seats.");

			int redShirts = markPending ? Math.Min(_pendingUnhappiness, unhappy) : 0;
			int index = 0;

			for (int i = 0; i < happy; i++)
			{
				ct.Citizens[index] = CitizenByIndex(index, Citizen.HappyMale);
				index++;
			}

			for (int i = 0; i < content; i++)
			{
				ct.Citizens[index] = CitizenByIndex(index, Citizen.ContentMale);
				index++;
			}

			// The first red shirts of the unhappy block are recoloured, so they stand at its left edge.
			// The order is display only: every stage rebuilds this array from the counts, and the refill
			// works on the counts alone.
			for (int i = 0; i < unhappy; i++)
			{
				Citizen type = i < redShirts ? Citizen.RedShirtMale : Citizen.UnhappyMale;
				ct.Citizens[index] = CitizenByIndex(index, type);
				index++;
			}

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == _city.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}
	}
}
