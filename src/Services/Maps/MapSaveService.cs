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
	public class MapSaveService(
		IAtomicFileReplacementService? atomicFileReplacementService = null,
		ILegacyMapSaveCompatibilityService? legacyMapSaveCompatibilityService = null,
		IMapEditor? mapEditor = null,
		IEnumerable<ICivilization>? civilizations = null) : IMapSaveService
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

		/// <summary>
		/// Decides whether the current map still fits the reduced legacy <c>*.map</c> format.
		/// </summary>
		private readonly ILegacyMapSaveCompatibilityService _legacyMapSaveCompatibilityService = legacyMapSaveCompatibilityService ?? new LegacyMapSaveCompatibilityService();

		private readonly IMapEditor? _mapEditor = mapEditor;
		private readonly IEnumerable<ICivilization>? _civilizations = civilizations;

		// Resolved lazily instead of in the constructor: Map.Instance/Common.Civilizations touch static
		// state (Common's static constructor reflects over and instantiates every advance/building/wonder
		// and requires a registered IRuntime). Eagerly resolving here would force that on every
		// MapSaveService construction, including in unit tests that never touch the live map.
		private IMapEditor MapEditor => _mapEditor ?? Map.Instance;

		private IEnumerable<ICivilization> Civilizations => _civilizations ?? Common.Civilizations;

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
			MapDto mapDto = mapMapper.ToDto(MapEditor);
			mapDto.MapSeed = unchecked((uint)Map.Instance.TerrainMasterWord);
			mapDto.StartPositions = BuildStartPositions(Civilizations, MapEditor);

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

		/// <inheritdoc/>
		public void SaveLegacyMap(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

			Map.Instance.SaveMap(filePath);
		}

		/// <inheritdoc/>
		public LegacyMapSaveCompatibilityResult GetLegacyMapCompatibility()
		{
			return _legacyMapSaveCompatibilityService.Evaluate(BuildLegacyCompatibilitySnapshot(Map.Instance, Civilizations));
		}

		/// <summary>
		/// Builds a <see cref="LegacyMapSaveCompatibilitySnapshot"/> from the given map and civilizations.
		/// </summary>
		/// <remarks>
		/// Takes its inputs as parameters (rather than reading <c>Map.Instance</c>/<c>Common.Civilizations</c>
		/// directly) so the snapshot construction can be unit tested in isolation.
		/// </remarks>
		/// <param name="map">The map to inspect.</param>
		/// <param name="civilizations">The civilizations whose start positions are counted.</param>
		/// <returns>A snapshot of the legacy-relevant map facts.</returns>
		internal static LegacyMapSaveCompatibilitySnapshot BuildLegacyCompatibilitySnapshot(Map map, IEnumerable<ICivilization> civilizations)
		{
			ArgumentNullException.ThrowIfNull(map);

			bool hasPollution = false;
			bool hasFortress = false;
			foreach (ITile tile in map.AllTiles())
			{
				hasPollution |= tile.Pollution;
				hasFortress |= tile.Fortress;
				if (hasPollution && hasFortress)
				{
					break;
				}
			}

			int startPositionCount = BuildStartPositions(civilizations, map)?.Count ?? 0;

			return new LegacyMapSaveCompatibilitySnapshot(
				map.Width,
				map.Height,
				unchecked((uint)map.TerrainMasterWord),
				startPositionCount,
				hasPollution,
				hasFortress);
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
