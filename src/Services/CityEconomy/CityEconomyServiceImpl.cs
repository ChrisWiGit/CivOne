using System;
using CivOne.Buildings;
using CivOne.Wonders;
using UniversityBuilding = CivOne.Buildings.University;

namespace CivOne
{
	/// <summary>
	/// Service implementation for calculating city economy breakdowns.
	/// Tight coupling to City because this service is an internal implementation detail of City and relies on City internals for its calculations. 
	/// </summary>
	internal sealed class CityEconomyServiceImpl(City city, IGame game) : ICityEconomyService
	{
		// Testing skipped because this method is an orchestration of pure calculations that are individually tested in CityEconomyServiceImplTests.
		private readonly City _city = city;
		private readonly IGame _game = game;

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
				_city.EntertainerLuxuries);

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

		public short CalculateLuxuries(short tradeLuxuries, bool hasMarketPlace, bool hasBank, int entertainerLuxuries)
		{
			short luxuries = tradeLuxuries;
			if (hasMarketPlace) luxuries += (short)Math.Floor(luxuries * 0.5);
			if (hasBank) luxuries += (short)Math.Floor(luxuries * 0.5);
			luxuries += (short)entertainerLuxuries;
			return luxuries;
		}

		public short CalculateTaxes(short tradeTaxes, bool hasMarketPlace, bool hasBank, int taxmen)
		{
			int taxes = tradeTaxes;
			if (hasMarketPlace) taxes += (int)Math.Floor(taxes * 0.5);
			if (hasBank) taxes += (int)Math.Floor(taxes * 0.5);
			taxes += taxmen * 2;
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
