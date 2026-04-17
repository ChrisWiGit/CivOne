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

		static ICityEconomyService Create(City city, IGame game) =>
			new CityEconomyServiceImpl(city, game);
	}
}