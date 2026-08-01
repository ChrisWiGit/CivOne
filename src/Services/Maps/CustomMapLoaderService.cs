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
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using CivOne.Services.Random;
using CivOne.Services.Sorting;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Discovers and loads custom map files from <see cref="ISettings.MapsDirectory"/>.
	/// </summary>
	/// <remarks>
	/// Supported formats are <c>*.comap</c> YAML map files, which use a top-level
	/// <c>Map:</c> key matching <see cref="MapFileDto"/>, and legacy Civilization I
	/// <c>*.map</c> files.
	/// </remarks>
	/// <param name="settings">Provides the maps directory to scan.</param>
	/// <param name="randomService">
	/// Supplies the terrain seed used when a legacy <c>*.map</c> file is loaded.
	/// Resolved from <see cref="RandomServiceFactory"/> when not supplied.
	/// </param>
	/// <param name="naturalSortService">
	/// Orders map file names so that embedded numbers sort by value.
	/// Resolved from <see cref="NaturalSortServiceFactory"/> when not supplied.
	/// </param>
	internal class CustomMapLoaderService(ISettings settings, IRandomService? randomService = null, INaturalSortService? naturalSortService = null) : ICustomMapLoaderService
	{
		private static readonly string[] MapExtensions = ["*.comap", "*.map"];

		private const string LegacyMapExtension = ".map";

		/// <summary>
		/// Upper bound of the terrain seed, matching the value range used by map generation.
		/// </summary>
		private const int TerrainSeedMaxExclusive = 16;

		private readonly IRandomService? _randomService = randomService;
		private readonly INaturalSortService? _naturalSortService = naturalSortService;

		private IRandomService RandomService => _randomService ?? RandomServiceFactory.Create();

		private INaturalSortService NaturalSortService => _naturalSortService ?? NaturalSortServiceFactory.Create();

		/// <inheritdoc/>
		public IReadOnlyList<string> GetMapFiles()
		{
			string dir = settings.MapsDirectory;

			if (!Directory.Exists(dir))
			{
				return [];
			}

			return [.. MapExtensions
				.SelectMany(ext => Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly))
				.OrderBy(path => Path.GetFileNameWithoutExtension(path) ?? path, NaturalSortService)];
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions to log and return false.")]
		/// <inheritdoc/>
		public bool LoadMapFile(string filePath)
		{
			try
			{
				if (IsLegacyMapFile(filePath))
				{
					return LoadLegacyMapFile(filePath);
				}

				var mapFileDto = YamlReader
					.OfString(File.ReadAllText(filePath))
					.WithStandard()
					.WithIgnoreUnmatchedProperties()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<MapFileDto>();

				var mapMapper = new MapDtoMapper(
					new RuntimeMapFactory(Map.Instance),
					new RuntimeTileDtoMapper(Map.Instance, new RuntimeTerrainFactory()),
					0);

				mapMapper.FromDto(mapFileDto.Map);
				Map.Instance.SetStartPositions(ResolveStartPositions(mapFileDto.Map));
				Map.Instance.FinalizeYamlLoad();

				return true;
			}
			catch (Exception ex)
			{
				RuntimeHandler.Runtime.Log("CustomMapLoaderService: Failed to load map from '{0}': {1}", filePath, ex.Message);
				return false;
			}
		}

		/// <summary>
		/// Checks whether the file is a legacy Civilization I <c>*.map</c> file.
		/// </summary>
		/// <param name="filePath">Full path to the map file.</param>
		/// <returns><see langword="true"/> for <c>*.map</c> files.</returns>
		private static bool IsLegacyMapFile(string filePath)
			=> Path.GetExtension(filePath).Equals(LegacyMapExtension, StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Loads a legacy Civilization I <c>*.map</c> file.
		/// </summary>
		/// <remarks>
		/// The legacy format stores no terrain seed, so a random one is used.
		/// It also stores no start positions; they are calculated when the game starts.
		/// </remarks>
		/// <param name="filePath">Full path to the <c>*.map</c> file.</param>
		/// <returns><see langword="true"/> on success; <see langword="false"/> if the file does not exist.</returns>
		private bool LoadLegacyMapFile(string filePath)
		{
			if (!File.Exists(filePath))
			{
				RuntimeHandler.Runtime.Log("CustomMapLoaderService: Map file '{0}' does not exist", filePath);
				return false;
			}

			Map.Instance.LoadMap(filePath, RandomService.NextInt(TerrainSeedMaxExclusive));
			return true;
		}

		internal static Dictionary<Civilization, MapLocation>? ResolveStartPositions(MapDto mapDto)
		{
			ArgumentNullException.ThrowIfNull(mapDto);

			if (mapDto.StartPositions == null || mapDto.StartPositions.Count == 0)
			{
				return null;
			}

			int width = mapDto.Tiles.Width();
			int height = mapDto.Tiles.Height();
			Dictionary<Civilization, MapLocation> resolvedStartPositions = [];

			foreach ((string civilizationName, MapLocation location) in mapDto.StartPositions)
			{
				if (!Enum.TryParse(civilizationName, ignoreCase: false, out Civilization civilization))
				{
					throw new FormatException($"Unknown civilization start-position key '{civilizationName}'.");
				}

				if (civilization == Civilization.Barbarians)
				{
					throw new FormatException("Barbarians cannot define a custom start position.");
				}

				if (location.X >= width || location.Y >= height)
				{
					throw new FormatException(
						$"Start position for '{civilizationName}' is outside the map bounds ({location.X},{location.Y}) for map size {width}x{height}.");
				}

				resolvedStartPositions[civilization] = new MapLocation(location);
			}

			return resolvedStartPositions;
		}
	}
}
