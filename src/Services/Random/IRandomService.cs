namespace CivOne.Services.Random
{
	public interface IRandomService
	{
		int Next(int max);
		int Next(int min, int max);
		bool Hit(int percent);
	}
}
