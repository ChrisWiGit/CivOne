using CivOne.Tiles;

namespace CivOne.Services
{
	public interface ILandValueCalculatorService
	{
		void CalculateLandValue(ITile[,] tiles);
	}
}
