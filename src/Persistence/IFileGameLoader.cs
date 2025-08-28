namespace CivOne.Persistence
{

	public interface IFileGameLoader
	{
		IGame Load<TLoader>(string filePath) where TLoader : IGameLoader;
	}
}