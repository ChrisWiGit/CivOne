using CivOne.Buildings;
using CivOne.Tiles;
using CivOne.Wonders;

namespace CivOne
{
	public interface ICityBuildings
	{
		bool HasBuilding<T>() where T : IBuilding;

		bool HasWonder<T>() where T : IWonder;
	}

}
