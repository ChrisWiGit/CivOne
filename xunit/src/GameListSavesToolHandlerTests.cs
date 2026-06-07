using System;
using System.IO;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameListSavesToolHandlerTests : IDisposable
	{
		private readonly string _tempFolder;
		private readonly RuntimeSettings _runtimeSettings;
		private readonly MockRuntime _mockRuntime;
		private readonly GameListSavesToolHandler _testee;

		public GameListSavesToolHandlerTests()
		{
			_tempFolder = Path.Combine(Path.GetTempPath(), $"civone-mcp-saves-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempFolder);

			_runtimeSettings = new RuntimeSettings();
			_runtimeSettings["mcp-saves"] = _tempFolder;
			_mockRuntime = new MockRuntime(_runtimeSettings);
			_testee = new GameListSavesToolHandler(_mockRuntime, new JsonSaveGameStateWriter(), 32000);
		}

		[Fact]
		public void HandleInvalidCosFilesAreOmittedFromResponse()
		{
			CreateValidCos(Path.Combine(_tempFolder, "valid.cos"));
			File.WriteAllText(Path.Combine(_tempFolder, "broken.cos"), "not: yaml: save");

			using JsonDocument args = JsonDocument.Parse("{}");
			McpRequest request = new("2.0", "list-1", "game_list_saves", args.RootElement.Clone(), null);

			McpResponse response = _testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			JsonElement saves = payload.RootElement.GetProperty("data").GetProperty("saves");
			Assert.Single(saves.EnumerateArray());
			Assert.Equal("valid.cos", saves[0].GetProperty("fileName").GetString());
		}

		private static void CreateValidCos(string filePath)
		{
			var saveGuid = Guid.NewGuid();
			SaveGame1FileRootDto root = new()
			{
				FormatVersion = SaveGame1FileRootDto.CurrentFormatVersion,
				Meta = new SaveGameMetaDataDto
				{
					GameStartedAt = DateTimeOffset.UtcNow.ToString("O"),
					GameVersion = "test",
					PlayDurationMinutes = 0,
					DisplayName = "Test Save",
					SaveGuid = saveGuid
				},
				GameState = new GameStateDto
				{
					Difficulty = DifficultyLevel.Chieftain,
					GameTurn = 42,
					HumanPlayer = 0,
					CurrentPlayer = 0,
					Players = [],
					Map = null
				}
			};

			string yaml = YamlWriter
				.Of(root)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			File.WriteAllText(filePath, yaml);
		}

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(_tempFolder))
					Directory.Delete(_tempFolder, true);
			}
			catch
			{
				// ignore test cleanup errors
			}

			_mockRuntime.Dispose();
			RuntimeHandler.Wipe();
		}
	}
}
