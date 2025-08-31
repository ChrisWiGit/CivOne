using CivOne.Tiles;

namespace CivOne.Services
{
	public interface IHutGeneratorService
	{
		void PlaceHuts(ITile[,] tiles);
	}
}
