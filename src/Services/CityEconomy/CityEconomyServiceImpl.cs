using System;
using CivOne.Buildings;
using CivOne.Wonders;
using UniversityBuilding = CivOne.Buildings.University;

namespace CivOne
{
	#pragma warning disable CA1822 // Mark members as static
	/// <summary>
	/// Service implementation for calculating city economy breakdowns.
	/// Tight coupling to City because this service is an internal implementation detail of City and relies on City internals for its calculations.
	///
	/// The specialists are added to the luxuries and the taxes before the marketplace and the bank raise the
	/// result, so a specialist is worth more in a city that has those buildings.
	/// </summary>
	internal sealed class CityEconomyServiceImpl(City city, IGame game) : ICityEconomyService
	{
		// Testing skipped because this method is an orchestration of pure calculations that are individually tested in CityEconomyServiceImplTests.
		private readonly City _city = city;
		private readonly IGame _game = game;

		/// <summary>
		/// Gets the luxury points the entertainers contribute, before the marketplace and the bank raise them.
		///
		/// CivOne's <see cref="CivOne.City.EntertainerLuxuries"/> is three points per entertainer, the
		/// marketplace bonus already folded in. This service raises the specialists with the buildings itself,
		/// so it starts from the unraised two points, or the bonus would apply twice.
		/// </summary>
		private int EntertainerLuxuryPoints => _city.Entertainers * 2;

		public CityEconomyBreakdown CalculateBreakdown()
		{
			int tradeTotal = _city.RawTradeTotal;
			int totalTrade = tradeTotal + _city.TradingCitiesSumValue;

			short tradeTaxes = CalculateTradeTaxes(totalTrade, _city.CityOwnerPlayer.TaxesRate);
			short tradeLuxuries = CalculateTradeLuxuries(totalTrade, tradeTaxes, _city.CityOwnerPlayer.TaxesRate, _city.CityOwnerPlayer.LuxuriesRate);
			short tradeScience = CalculateTradeScience(totalTrade, tradeLuxuries, tradeTaxes);

			short luxuries = CalculateLuxuries(
				tradeLuxuries,
				_city.HasBuilding<MarketPlace>(),
				_city.HasBuilding<Bank>(),
				EntertainerLuxuryPoints);

			short totalTaxes = CalculateTaxes(
				tradeTaxes,
				_city.HasBuilding<MarketPlace>(),
				_city.HasBuilding<Bank>(),
				_city.Taxmen);

			bool hasSeti = _city.CityOwnerPlayer.HasWonder<SETIProgram>();
			bool hasNewton = !_game.WonderObsolete<IsaacNewtonsCollege>() && _city.CityOwnerPlayer.HasWonder<IsaacNewtonsCollege>() && !hasSeti;
			bool hasCopernicus = !_game.WonderObsolete<CopernicusObservatory>() && _city.HasWonder<CopernicusObservatory>();

			short totalScience = CalculateScience(
				tradeScience,
				_city.HasBuilding<Library>(),
				_city.HasBuilding<UniversityBuilding>(),
				hasSeti,
				hasNewton,
				hasCopernicus,
				_city.Scientists);

			return new CityEconomyBreakdown(
				tradeTotal,
				totalTrade,
				tradeScience,
				tradeLuxuries,
				tradeTaxes,
				luxuries,
				totalTaxes,
				totalScience);
		}

		public short CalculateTradeTaxes(int totalTrade, int taxesRate)
		{
			// Truncate taxes toward zero to avoid overcharging the player when TotalTrade is low and TaxesRate is high.
			return (short)Math.Truncate((double)totalTrade * taxesRate / 10);
		}

		public short CalculateTradeLuxuries(int totalTrade, short tradeTaxes, int taxesRate, int luxuriesRate)
		{
			if (taxesRate >= 10)
			{
				return 0;
			}

			return (short)Math.Round((double)(totalTrade - tradeTaxes) / (10 - taxesRate) * luxuriesRate, MidpointRounding.AwayFromZero);
		}

		public short CalculateTradeScience(int totalTrade, short tradeLuxuries, short tradeTaxes)
		{
			return (short)Math.Max(0, totalTrade - tradeLuxuries - tradeTaxes);
		}

		/// <summary>
		/// Gets the luxuries of the city, with the entertainers added before the marketplace and the bank
		/// raise the result.
		/// </summary>
		/// <param name="tradeLuxuries">The luxuries the trade produced.</param>
		/// <param name="hasMarketPlace">Whether the city has a marketplace.</param>
		/// <param name="hasBank">Whether the city has a bank.</param>
		/// <param name="entertainerLuxuries">The luxuries the entertainers produced.</param>
		/// <returns>The luxuries of the city.</returns>
		public short CalculateLuxuries(short tradeLuxuries, bool hasMarketPlace, bool hasBank, int entertainerLuxuries)
		{
			int luxuries = tradeLuxuries + entertainerLuxuries;

			if (hasMarketPlace)
			{
				luxuries += luxuries / 2;
			}

			if (hasBank)
			{
				luxuries += luxuries / 2;
			}

			return (short)Math.Min(short.MaxValue, luxuries);
		}

		/// <summary>
		/// Gets the taxes of the city, with the taxmen added before the marketplace and the bank raise the
		/// result.
		/// </summary>
		/// <param name="tradeTaxes">The taxes the trade produced.</param>
		/// <param name="hasMarketPlace">Whether the city has a marketplace.</param>
		/// <param name="hasBank">Whether the city has a bank.</param>
		/// <param name="taxmen">The number of taxmen in the city.</param>
		/// <returns>The taxes of the city.</returns>
		public short CalculateTaxes(short tradeTaxes, bool hasMarketPlace, bool hasBank, int taxmen)
		{
			int taxes = tradeTaxes + (taxmen * 2);

			if (hasMarketPlace)
			{
				taxes += taxes / 2;
			}

			if (hasBank)
			{
				taxes += taxes / 2;
			}

			return (short)Math.Min(short.MaxValue, taxes);
		}

		public short CalculateScience(
			short tradeScience,
			bool hasLibrary,
			bool hasUniversity,
			bool hasSeti,
			bool hasNewton,
			bool hasCopernicus,
			int scientists)
		{
			double science = tradeScience;
			double libUniFactor = hasNewton ? 1.66 : 1.5;

			if (hasLibrary)
			{
				science *= libUniFactor;
			}
			if (hasUniversity)
			{
				science *= libUniFactor;
			}
			if (hasSeti)
			{
				science *= 1.5;
			}

			science += scientists * 2;

			if (hasCopernicus)
			{
				science *= 2.0;
			}

			return (short)Math.Min((int)Math.Round(science), short.MaxValue);
		}
	}
}
