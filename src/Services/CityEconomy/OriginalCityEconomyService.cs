using System;
using CivOne.Buildings;

namespace CivOne
{
	/// <summary>
	/// Calculates a city's economy the way the original game does.
	///
	/// Only one thing differs from <see cref="CityEconomyServiceImpl"/>, and it differs twice: the original
	/// adds the specialists to the luxuries and the taxes <em>before</em> the marketplace and the bank raise
	/// them, so a specialist is worth more in a city that has those buildings. CivOne adds them afterwards,
	/// which leaves the specialist at a flat two points wherever it stands.
	///
	/// This class exists so the corrected order can be switched on together with the original city happiness
	/// model, leaving the shipped behaviour and its tests untouched until then.
	/// </summary>
	/// <param name="city">The city to calculate for.</param>
	/// <param name="game">The game the city belongs to.</param>
	internal sealed class OriginalCityEconomyService(City city, IGame game) : CityEconomyServiceImpl(city, game)
	{
		/// <summary>
		/// Gets two luxury points per entertainer, the value the original uses before the buildings raise it.
		///
		/// <see cref="CityEconomyServiceImpl.EntertainerLuxuryPoints"/> returns three, because CivOne folds the
		/// marketplace bonus into the constant. Raising that by another fifty percent here would grant the
		/// bonus twice, so this class starts from the unraised two.
		/// </summary>
		protected override int EntertainerLuxuryPoints => City.Entertainers * 2;

		/// <summary>
		/// Adds the entertainers to the luxuries before the marketplace and the bank raise them.
		/// </summary>
		/// <param name="tradeLuxuries">The luxuries the trade produced.</param>
		/// <param name="hasMarketPlace">Whether the city has a marketplace.</param>
		/// <param name="hasBank">Whether the city has a bank.</param>
		/// <param name="entertainerLuxuries">The luxuries the entertainers produced.</param>
		/// <returns>The luxuries of the city.</returns>
		public override short CalculateLuxuries(short tradeLuxuries, bool hasMarketPlace, bool hasBank, int entertainerLuxuries)
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
		/// Adds the taxmen to the taxes before the marketplace and the bank raise them.
		/// </summary>
		/// <param name="tradeTaxes">The taxes the trade produced.</param>
		/// <param name="hasMarketPlace">Whether the city has a marketplace.</param>
		/// <param name="hasBank">Whether the city has a bank.</param>
		/// <param name="taxmen">The number of taxmen in the city.</param>
		/// <returns>The taxes of the city.</returns>
		public override short CalculateTaxes(short tradeTaxes, bool hasMarketPlace, bool hasBank, int taxmen)
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
	}
}
