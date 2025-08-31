namespace CivOne.Services
{
    /// <summary>
    /// Service provider for map-related services.
    /// This provider is used for dependency injection everywhere in the code that needs these services.
    /// If we decide to change a service implementation, we only need to change it here.
    /// Can be made also using settings for dynamic service resolution.
    /// </summary>
    public static class MapServiceProvider
    {
        public static IHutGeneratorService GetHutProvider(int randomSeed)
        {
            return new HutGeneratorServiceImpl(randomSeed);
        }

        public static ILandValueCalculatorService GetLandValueCalculator()
        {
            return new LandValueCalculatorServiceImpl();
        }

        public static ITileConverterService GetTileConverterService(int random)
        {
            return new TileConverterServiceImpl(Map.HEIGHT, random);
        }
    }
}