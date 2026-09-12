namespace CivOne
{
	internal readonly struct CityEconomyBreakdown(
		int tradeTotal,
		int totalTrade,
		short tradeScience,
		short tradeLuxuries,
		short tradeTaxes,
		short luxuries,
		short taxes,
		short science)
	{
		public int TradeTotal { get; } = tradeTotal;
		public int TotalTrade { get; } = totalTrade;
		public short TradeScience { get; } = tradeScience;
		public short TradeLuxuries { get; } = tradeLuxuries;
		public short TradeTaxes { get; } = tradeTaxes;
		public short Luxuries { get; } = luxuries;
		public short Taxes { get; } = taxes;
		public short Science { get; } = science;
	}

	internal interface ICityEconomyService
	{
		CityEconomyBreakdown CalculateBreakdown();

		/// <summary>
		/// Creates the economy service for a city.
		/// The same setting that selects the original city happiness model also selects the original order of
		/// the luxury and tax calculation, so the city screen and the reports can never disagree.
		/// </summary>
		/// <param name="city">The city to calculate for.</param>
		/// <param name="game">The game the city belongs to.</param>
		/// <returns>The economy service to use.</returns>
		static ICityEconomyService Create(City city, IGame game) =>
			Settings.Instance.OriginalHappinessModel
				? new OriginalCityEconomyService(city, game)
				: new CityEconomyServiceImpl(city, game);
	}
}