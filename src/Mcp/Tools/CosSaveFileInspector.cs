using System;
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
		public SaveGameMetaDataDto Meta { get; init; }
		public GameStateDto GameState { get; init; }
	}

	internal static class CosSaveFileInspector
	{
		public static bool TryInspect(string cosFilePath, out CosSaveFileInspection inspection)
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
				SaveGameFileRootDto root = YamlReader
					.OfString(yaml)
					.WithStandard()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<SaveGameFileRootDto>();

				if (root?.GameState == null)
					return false;

				inspection = new CosSaveFileInspection
				{
					FormatVersion = root.FormatVersion,
					SaveGuid = root.SaveGuid,
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
