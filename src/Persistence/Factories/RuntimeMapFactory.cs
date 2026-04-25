using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.Persistence.Factories
{
	/// <summary>
	/// Runtime implementation of <see cref="IMapFactory"/> for the YAML load path.
	/// Initializes <see cref="Map.Instance"/> for tile restoration without triggering
	/// terrain generation, hut placement, or land value calculation - all of which
	/// are already serialized in the YAML file.
	/// </summary>
	public class RuntimeMapFactory(Map map) : IMapFactory
	{
		private readonly Map _map = map;

		public IMapTiles CreateMap(int width, int height, uint terrainSeed)
		{
			_map.InitializeForYamlLoad(width, height, (int)terrainSeed);
			return _map;
		}
	}
}
