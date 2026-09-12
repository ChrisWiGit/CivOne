using System;
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
	/// <summary>
	/// Calculates city happiness the way the original game does.
	///
	/// Two things separate this from <see cref="CityCitizenService"/>.
	/// The base unhappiness carries an empire size penalty that grows with the number of cities and with the
	/// government, instead of the fixed city count steps CivOne used so far.
	/// And unhappiness that does not fit into the city is parked outside it and flows back whenever a
	/// modifier lowers the visible unhappiness again, so a large empire needs more temples, colosseums and
	/// luxuries than the visible unhappy count suggests.
	///
	/// The class exists next to the model CivOne shipped with and is selected by a setting, so the old model
	/// and its tests keep running until the corrected one has been play tested.
	/// </summary>
	internal sealed class OriginalCityCitizenService : CityCitizenService
	{
		private readonly PendingUnhappinessRefill _refill;
		private readonly int? _luxuryRate;

		/// <summary>
		/// Unhappiness that does not fit into the city.
		/// Reset at the start of every calculation, see <see cref="Stage1"/>.
		/// </summary>
		private int _pendingUnhappiness;

		/// <summary>
		/// Creates the corrected happiness model for one city.
		/// </summary>
		/// <param name="city">The city to calculate.</param>
		/// <param name="cityBuildings">The buildings and wonders of the city.</param>
		/// <param name="game">The game state the calculation depends on.</param>
		/// <param name="specialists">The specialists of the city.</param>
		/// <param name="map">The map, used for the continent of the city.</param>
		/// <param name="refill">How the parked unhappiness flows back. Chosen by the factory.</param>
		public OriginalCityCitizenService(
			ICityBasic city,
			ICityBuildings cityBuildings,
			IGameCitizenDependency game,
			List<Citizen> specialists,
			IMap map,
			PendingUnhappinessRefill refill,
			int? luxuryRate = null) : base(city, cityBuildings, game, specialists, map)
		{
			ArgumentNullException.ThrowIfNull(refill);
			_refill = refill;
			_luxuryRate = luxuryRate;
		}

		/// <summary>
		/// Gets the share of trade that becomes luxuries, in tenths.
		/// Without a rate from the caller this is the rate the player has set, which is whatever the tax and
		/// science sliders leave over.
		/// </summary>
		internal int LuxuryRate => _luxuryRate
			?? Math.Clamp(10 - City.PlayerIntf.ScienceRate - City.PlayerIntf.TaxesRate, 0, 10);

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
			int trade = Math.Max(City.TradeTotalGross, 0);
			int corruption = Math.Clamp(City.LuxuryCorruption, 0, trade);

			// The 5 sits inside the numerator, so the product is rounded, not the quotient.
			int luxuries = Math.Clamp(((LuxuryRate * (trade - corruption)) + 5) / 10, 0, trade);

			luxuries += City.Entertainers * 2;

			if (CityBuildings.HasBuilding<MarketPlace>())
			{
				luxuries += luxuries / 2;
			}

			if (CityBuildings.HasBuilding<Bank>())
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
			int difficulty = Math.Clamp(GameState.Difficulty, 0, MaxPenaltyDifficulty);
			int governmentId = City.PlayerIntf.Government.Id;

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

			int cityCount = GameState.GetPlayer(City.CityOwnerPlayerIndex)?.Cities.Length ?? 0;

			// A city that is not part of the game has no slot; treat it as the first one.
			int cityIndex = Math.Max(GameState.GetCityIndex(City), 0);

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
			if (!GameState.IsHumanPlayer(City.CityOwnerPlayerIndex))
			{
				// The computer players carry neither the difficulty scaling nor the empire size penalty.
				return City.Size - 3;
			}

			return City.Size + GameState.Difficulty - 6 + EmpireSizePenalty();
		}

		/// <summary>
		/// Replaced by the empire size penalty of <see cref="BaseUnhappy"/>, which generalises the fixed
		/// city count steps of the base class to every difficulty and every government.
		/// </summary>
		/// <param name="ct">The citizen types, left untouched.</param>
		protected internal override void ApplyEmperorEffects(CitizenTypes ct)
		{
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
		protected internal override int Stage1(ref CitizenTypes ct)
		{
			_pendingUnhappiness = 0;

			int specialists = ct.elvis + ct.einstein + ct.taxman;
			int workersAvailable = Math.Max(City.Size - specialists, 0);

			int unhappyCount = Math.Max(BaseUnhappy(), 0);

			// Only what does not fit into the city itself is parked. The seats the specialists take up are
			// not part of that test: the original parks the empire size penalty that exceeds the city size,
			// and lets the normalisation squeeze the rest into the seats afterwards. Parking against the
			// seats instead would leave a surplus behind that shields the happy citizens from the very first
			// normalisation, which raises the happy count of every crowded city by one.
			_pendingUnhappiness = Math.Max(unhappyCount - City.Size, 0);

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
		protected internal override CitizenTypes Stage2(CitizenTypes ct)
		{
			int happy = Luxuries() / 2;
			return Normalise(ct, happy, ct.unhappy + ct.redShirt, markPending: false);
		}

		/// <inheritdoc/>
		protected internal override CitizenTypes Stage3(CitizenTypes ct)
		{
			return Normalise(base.Stage3(ct), markPending: false);
		}

		/// <summary>
		/// Applies the buildings of stage 3: the colosseum, the cathedral and the temple with the oracle.
		///
		/// Shakespeare’s Theatre and J.S. Bach’s Cathedral are deliberately missing here. The original applies
		/// them among the wonders in stage 5, after martial law and war weariness, so they are handled by
		/// <see cref="ApplyWonderEffects"/> instead. The base class applies all five in this stage and lets
		/// Shakespeare skip the rest, which this override does not do: in the original every building still
		/// takes effect, Shakespeare simply leaves nothing for them to convert two stages later.
		/// </summary>
		/// <param name="ct">The citizen types to change.</param>
		protected internal override void ApplyBuildingEffects(CitizenTypes ct)
		{
			int unhappyToContent = 0;

			if (CityBuildings.HasBuilding<Temple>())
			{
				// The temple is gated the way the cathedral is, only with two steps instead of one: Mysticism
				// makes it worth two, Ceremonial Burial alone one, and without either it has no effect.
				// All three branches are reachable. Ceremonial Burial is the temple's build prerequisite, so
				// the empty branch belongs to a captured city, whose temple stays idle until its new owner
				// researches the advance. Mysticism requires Ceremonial Burial, so "Mysticism without
				// Ceremonial Burial" is the one combination that cannot occur.
				bool hasMysticism = City.PlayerIntf.HasAdvance<Mysticism>();

				if (hasMysticism)
				{
					unhappyToContent += 2;
				}
				else if (City.PlayerIntf.HasAdvance<CeremonialBurial>())
				{
					unhappyToContent += 1;
				}

				// The oracle sits inside the temple block, so it needs a temple but no advance of its own.
				// It follows the temple's Mysticism step without inheriting the Ceremonial Burial gate.
				if (City.PlayerIntf.HasWonderEffect<Oracle>())
				{
					unhappyToContent += hasMysticism ? 2 : 1;
					ct.Wonders.Add(new Oracle());
				}

				ct.Buildings.Add(new Temple());
			}

			if (CityBuildings.HasBuilding<Colosseum>())
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

			if (CityBuildings.HasBuilding<Cathedral>())
			{
				ct.Buildings.Add(new Cathedral());
			}

			UnhappyToContent(ct.Citizens, unhappyToContent);
		}

		/// <summary>
		/// Applies the wonders of stage 5, in the order the original uses: the Hanging Gardens and the Cure
		/// For Cancer make a citizen happy each, then Shakespeare’s Theatre clears the unhappiness of its own
		/// city, then J.S. Bach’s Cathedral takes two more away across the continent.
		///
		/// Placing Shakespeare here rather than with the buildings matters: stage 4 sits in between, so in the
		/// original the theatre wipes out the war weariness of its city, while applying it two stages earlier
		/// would let the weariness make citizens unhappy again afterwards.
		/// </summary>
		/// <param name="ct">The citizen types to change.</param>
		protected internal override void ApplyWonderEffects(CitizenTypes ct)
		{
			base.ApplyWonderEffects(ct);

			(_, _, int unhappy, int redShirt) = CountCitizenTypes(ct.Citizens);

			if (CityBuildings.HasWonder<ShakespearesTheatre>() &&
				!GameState.WonderObsolete<ShakespearesTheatre>())
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
		protected internal override CitizenTypes Stage4(CitizenTypes ct, int initialContent)
		{
			if (!City.PlayerIntf.RepublicDemocratic)
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
		/// Counts how weary the citizens are of the troops that are away from home.
		///
		/// A republic loses one citizen per unit abroad and a democracy two. Women’s Suffrage removes one of
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
			IUnit[] unitsAway = [.. GameState.GetUnits()
				.Where(u => u.IsHome(City) && u.Attack > 0
					&& (u.UnitCategory == UnitClass.Air || new Point(u.X, u.Y) != City.Location))];

			ct.MarshallLawUnits.AddRange(unitsAway);

			int unhappyPerUnit = City.PlayerIntf.HasWonderEffect<WomensSuffrage>() ? 0 : 1;
			if (City.PlayerIntf.Government is Governments.Democracy)
			{
				unhappyPerUnit++;
			}

			return unitsAway.Length * unhappyPerUnit;
		}

		/// <summary>
		/// Gets how much unhappiness a cathedral takes away.
		///
		/// The whole block sits behind a check on the advance Religion. Religion is also what allows a
		/// cathedral to be built, so the check decides one case only, and it is a real one: a captured city
		/// brings the building without bringing the advance. Such a cathedral is idle until its new owner
		/// researches Religion, and then it works retroactively.
		///
		/// Michelangelo’s Chapel raises the effect for every city of its owner, wherever those cities stand:
		/// the chapel is not bound to a continent, only J.S. Bach’s Cathedral is.
		/// </summary>
		/// <returns>The unhappiness the cathedral removes, or 0 when there is none.</returns>
		internal override int CathedralDelta()
		{
			if (!CityBuildings.HasBuilding<Cathedral>())
			{
				return 0;
			}

			if (!City.PlayerIntf.HasAdvance<Religion>())
			{
				return 0;
			}

			return City.PlayerIntf.HasWonderEffect<MichelangelosChapel>() ? 6 : 4;
		}

		/// <inheritdoc/>
		protected internal override CitizenTypes Stage5(CitizenTypes ct)
		{
			return Normalise(base.Stage5(ct), markPending: true);
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
		/// Stage 2 needs this, because the original sets the happy count from the luxuries rather than
		/// deriving it from the citizens.
		/// </summary>
		/// <param name="ct">The citizen types of the finished stage.</param>
		/// <param name="happy">The happy count to normalise.</param>
		/// <param name="unhappy">The unhappy count to normalise.</param>
		/// <param name="markPending">Whether the pending unhappiness is drawn as red shirts.</param>
		/// <returns>The corrected citizen types.</returns>
		private CitizenTypes Normalise(CitizenTypes ct, int happy, int unhappy, bool markPending)
		{
			int seats = Math.Max(City.Size - SpecialistCount, 0);

			int pending = _pendingUnhappiness;

			_refill(ref unhappy, ref pending);

			happy = Math.Clamp(happy, 0, City.Size);
			unhappy = Math.Clamp(unhappy, 0, City.Size);

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
		/// Happy citizens come first, then content ones, then unhappy ones, which is the order the base
		/// class produces as well. The specialists keep the tail of the array and are never touched.
		/// </summary>
		/// <param name="ct">The citizen types to write into.</param>
		/// <param name="happy">The happy citizen count.</param>
		/// <param name="unhappy">The unhappy citizen count.</param>
		/// <param name="markPending">Whether the parked unhappiness is drawn as red shirts.</param>
		/// <returns>The citizen types with the counts recounted from the array.</returns>
		private CitizenTypes WriteCitizens(CitizenTypes ct, int happy, int unhappy, bool markPending)
		{
			int seats = Math.Max(City.Size - SpecialistCount, 0);
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

			// The citizens standing for the parked unhappiness are the last ones, so luxuries and buildings
			// reach the ordinary unhappy citizens first.
			for (int i = 0; i < unhappy; i++)
			{
				Citizen type = i >= unhappy - redShirts ? Citizen.RedShirtMale : Citizen.UnhappyMale;
				ct.Citizens[index] = CitizenByIndex(index, type);
				index++;
			}

			(ct.happy, ct.content, ct.unhappy, ct.redShirt) = CountCitizenTypes(ct.Citizens);

			DebugService.Assert(ct.Sum() == City.Size);
			DebugService.Assert(ct.Valid());
			return ct;
		}
	}
}
