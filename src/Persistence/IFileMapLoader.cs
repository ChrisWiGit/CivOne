namespace CivOne.Persistence
{

	public interface IFileMapLoader
	{
		IMap Load(string filePath);
	}
}