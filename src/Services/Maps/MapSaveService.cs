// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Text;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using CivOne.Tiles;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Writes the current map (terrain and per-civilization start positions) to a standalone
	/// <c>*.comap</c> YAML file, reusing the same <see cref="MapDtoMapper"/>/<see cref="MapDtoTileDtoYamlConverter"/>
	/// pipeline that full game saves and <see cref="CustomMapLoaderService"/> already use for reading.
	/// </summary>
	/// <remarks>
	/// Builds its own minimal <see cref="MapDtoMapper"/> instead of going through
	/// <c>YamlMapperDependenciesFactory</c> (used by full game saves), since that factory also
	/// constructs player/unit/city mappers and re-scans the assembly for civilizations, units,
	/// advances and governments via reflection - all irrelevant for a map-only save and expensive
	/// enough to make the save dialog feel unresponsive for a no-op cost.
	/// </remarks>
	public class MapSaveService(IAtomicFileReplacementService? atomicFileReplacementService = null) : IMapSaveService
	{
		/// <summary>
		/// Performs the actual file write through a write-to-temp-then-swap strategy.
		///
		/// A <c>*.comap</c> save typically overwrites an existing map file the user wants to keep.
		/// Writing straight to the destination would corrupt or truncate that file if the write threw
		/// part way through (bad YAML, full disk, crash), destroying the previous map. Delegating to
		/// <see cref="IAtomicFileReplacementService"/> means the destination only ever changes once the
		/// full YAML is safely on disk: on failure the original file stays intact.
		/// </summary>
		private readonly IAtomicFileReplacementService _atomicFileReplacementService = atomicFileReplacementService ?? new AtomicFileReplacementService();

		private sealed class UnusedMapFactory : IMapFactory
		{
			public IMapTiles CreateMap(int width, int height, uint terrainSeed) => throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}

		private sealed class UnusedTileFactory : ITileFactory
		{
			public ITile CreateTile(int x, int y, Terrain terrain) => throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}

		/// <inheritdoc/>
		public void SaveCivOneMap(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

			MapDtoMapper mapMapper = new(new UnusedMapFactory(), new DefaultTileDtoMapper(new UnusedTileFactory()));
			MapDto mapDto = mapMapper.ToDto(Map.Instance);
			mapDto.MapSeed = unchecked((uint)Map.Instance.TerrainMasterWord);
			mapDto.StartPositions = BuildStartPositions(Common.Civilizations, Map.Instance);

			MapFileDto mapFileDto = new() { Map = mapDto };
			string yaml = YamlWriter.Of(mapFileDto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			_atomicFileReplacementService.ReplaceFile(filePath, stream =>
			{
				byte[] bytes = Encoding.UTF8.GetBytes(yaml);
				stream.Write(bytes, 0, bytes.Length);
			});
		}

		/// <summary>
		/// Filters <paramref name="civilizations"/> down to the ones with a custom start position set
		/// on <paramref name="mapEditor"/>, keyed by civilization name for YAML serialization.
		/// </summary>
		/// <remarks>
		/// Takes its inputs as parameters (rather than reading <c>Common.Civilizations</c>/<c>Map.Instance</c>
		/// directly) so the Barbarian-skip filter can be unit tested in isolation.
		/// </remarks>
		internal static Dictionary<string, MapLocation>? BuildStartPositions(IEnumerable<ICivilization> civilizations, IMapEditor mapEditor)
		{
			Dictionary<string, MapLocation> positions = [];
			foreach (ICivilization civilization in civilizations)
			{
				if (civilization is Barbarian)
				{
					continue;
				}

				if (mapEditor.TryGetStartPosition(civilization, out MapLocation? location) && location != null)
				{
					positions[((Civilization)civilization.Id).ToString()] = location;
				}
			}

			return positions.Count > 0 ? positions : null;
		}
	}
}
