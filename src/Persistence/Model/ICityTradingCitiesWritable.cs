namespace CivOne.Persistence.Model
{
    public interface ICityTradingCitiesWritable : ICity, ICityMapper
    {
        new ICity[] TradingCities { get; set; }
    }
}
