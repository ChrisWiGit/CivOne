namespace CivOne.Persistence
{
	public interface IMapLoaderProvider
	{
		IMapLoader GetLoader<T>() where T : IMapLoader;
	}
}