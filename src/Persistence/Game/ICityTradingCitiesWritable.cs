using CivOne.Persistence.Game;

namespace CivOne.Persistence.Game
{
    public interface ICityTradingCitiesWritable : ICity, ICityMapper
    {
        new ICity[] TradingCities { get; set; }
    }
}
