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

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Discovers and loads custom map files from <see cref="ISettings.MapsDirectory"/>.
	/// </summary>
	/// <remarks>
	/// Supported formats are <c>*.comap</c> and <c>*.cos</c> YAML map files.
	/// Both use a top-level <c>Map:</c> key matching <see cref="MapFileDto"/>.
	/// </remarks>
	internal class CustomMapLoaderService(ISettings settings) : ICustomMapLoaderService
	{
		private static readonly string[] MapExtensions = ["*.comap", "*.cos"];

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
				.Order(StringComparer.OrdinalIgnoreCase)];
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions to log and return false.")]
		/// <inheritdoc/>
		public bool LoadMapFile(string filePath)
		{
			try
			{
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

		private static Dictionary<Civilization, MapLocation>? ResolveStartPositions(MapDto mapDto)
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
