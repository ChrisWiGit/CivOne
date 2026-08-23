using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;

namespace CivOne.Mcp.Tools
{
	internal sealed class CosSaveFileInspection
	{
		public uint? FormatVersion { get; init; }
		public Guid? SaveGuid { get; init; }
		public SaveGameMetaDataDto? Meta { get; init; } = new();
		public GameStateDto? GameState { get; init; } = new();
	}

	internal static class CosSaveFileInspector
	{
		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions is necessary to ensure that failure to inspect a .COS file does not crash the application, and that any exceptions are logged appropriately.")]
		public static bool TryInspect(string? cosFilePath, out CosSaveFileInspection? inspection)
		{
			inspection = null;
			if (string.IsNullOrWhiteSpace(cosFilePath) || !File.Exists(cosFilePath))
				return false;

			string yaml;
			try
			{
				yaml = File.ReadAllText(cosFilePath);
			}
			catch
			{
				return false;
			}

			try
			{
				SaveGame1FileRootDto root = YamlReader
					.OfString(yaml)
					.WithStandard()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<SaveGame1FileRootDto>();

				if (root?.GameState == null)
					return false;

				inspection = new CosSaveFileInspection
				{
					FormatVersion = root.FormatVersion,
					SaveGuid = root.Meta?.SaveGuid,
					Meta = root.Meta,
					GameState = root.GameState
				};
				return true;
			}
			catch
			{
				// legacy GameStateDto-only format fallback
			}

			try
			{
				GameStateDto legacy = YamlReader
					.OfString(yaml)
					.WithStandard()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<GameStateDto>();

				if (legacy == null)
					return false;

				inspection = new CosSaveFileInspection
				{
					FormatVersion = null,
					SaveGuid = null,
					Meta = null,
					GameState = legacy
				};
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
