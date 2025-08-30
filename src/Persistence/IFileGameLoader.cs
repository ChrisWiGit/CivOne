namespace CivOne.Persistence
{

	public interface IFileGameLoader
	{
		IGame Load(string filePath);
	}
}