using CivOne.Persistence;

namespace CivOne
{
	public class MapFactoryImpl : IMapFactory
	{
		public IMap Create(IGameData data)
		{
			var map = new Map();

			return map;
		}
	}
}