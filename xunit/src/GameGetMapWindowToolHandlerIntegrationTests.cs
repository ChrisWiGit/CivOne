using System.Text.Json;
using CivOne.Mcp.Automation;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using CivOne.Persistence.Factories;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameGetMapWindowToolHandlerIntegrationTests : TestsBase
	{
		[Fact]
		[Trait("Category", "IntegrationEarthYaml")]
		public void HandleFullMapViaMapSizeToolEncodedFormatIsNotTruncated()
		{
			// Arrange
			IGameStateDtoSnapshotProvider snapshotProvider = new GameStateDtoSnapshotProvider(
				new RuntimeHandlerGameTickProvider(),
				new GameStateHandler(),
				YamlMapperDependenciesFactory.CreateDefault());

			GameGetMapSizeToolHandler mapSizeTool = new(snapshotProvider, new JsonSaveGameStateWriter(), 32000);
			GameGetMapWindowToolHandler mapWindowTool = new(snapshotProvider, new JsonSaveGameStateWriter(), 32000);

			using JsonDocument emptyArgsDoc = JsonDocument.Parse("{}");
			McpRequest mapSizeRequest = new("2.0", "int-size-1", "game_get_map_size", emptyArgsDoc.RootElement.Clone(), null);

			// Act 1: read map size via MCP tool
			McpResponse mapSizeResponse = mapSizeTool.Handle(mapSizeRequest);

			McpToolCallResult mapSizeResult = Assert.IsType<McpToolCallResult>(mapSizeResponse.Result);
			using JsonDocument mapSizePayload = JsonDocument.Parse(Assert.Single(mapSizeResult.Content).Text);
			JsonElement mapSizeRoot = mapSizePayload.RootElement;
			Assert.True(mapSizeRoot.GetProperty("ok").GetBoolean());

			int width = mapSizeRoot.GetProperty("data").GetProperty("width").GetInt32();
			int height = mapSizeRoot.GetProperty("data").GetProperty("height").GetInt32();

			using JsonDocument fullWindowArgsDoc = JsonDocument.Parse($"{{\"x\":0,\"y\":0,\"width\":{width},\"height\":{height},\"format\":\"encoded\"}}");
			McpRequest mapWindowRequest = new("2.0", "int-window-1", "game_get_map_window", fullWindowArgsDoc.RootElement.Clone(), null);

			// Act 2: request complete map window
			McpResponse mapWindowResponse = mapWindowTool.Handle(mapWindowRequest);

			// Assert
			McpToolCallResult mapWindowResult = Assert.IsType<McpToolCallResult>(mapWindowResponse.Result);
			using JsonDocument mapWindowPayload = JsonDocument.Parse(Assert.Single(mapWindowResult.Content).Text);

			JsonElement root = mapWindowPayload.RootElement;
			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.False(root.GetProperty("truncated").GetBoolean());
			Assert.Equal("encoded", root.GetProperty("data").GetProperty("format").GetString());

			JsonElement rows = root.GetProperty("data").GetProperty("window").GetProperty("rows");
			Assert.Equal(height, rows.GetArrayLength());
			Assert.NotNull(rows[0].GetString());
			Assert.Equal(width * 2, rows[0].GetString()!.Length);

			Assert.Equal(width, root.GetProperty("data").GetProperty("mapSize").GetProperty("width").GetInt32());
			Assert.Equal(height, root.GetProperty("data").GetProperty("mapSize").GetProperty("height").GetInt32());
		}

		[Fact]
		[Trait("Category", "IntegrationEarthYaml")]
		public void HandleEncodedFormatWithEarthYamlSnapshotReturnsWindowRows()
		{
			// Arrange
			IGameStateDtoSnapshotProvider snapshotProvider = new GameStateDtoSnapshotProvider(
				new RuntimeHandlerGameTickProvider(),
				new GameStateHandler(),
				YamlMapperDependenciesFactory.CreateDefault());

			GameGetMapWindowToolHandler testee = new(snapshotProvider, new JsonSaveGameStateWriter(), 32000);

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":5,\"height\":3,\"format\":\"encoded\"}");
			McpRequest request = new("2.0", "int-1", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			JsonElement root = payload.RootElement;
			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.Equal("encoded", root.GetProperty("data").GetProperty("format").GetString());
			Assert.Equal("docs/YAML_TILE_ENCODING.md", root.GetProperty("meta").GetProperty("tileEncodingDocPath").GetString());

			JsonElement rows = root.GetProperty("data").GetProperty("window").GetProperty("rows");
			Assert.Equal(3, rows.GetArrayLength());
			Assert.NotNull(rows[0].GetString());
			Assert.Equal(10, rows[0].GetString()!.Length); // width 5 * 2 chars per tile
			Assert.NotNull(rows[1].GetString());
			Assert.Equal(10, rows[1].GetString()!.Length);
			Assert.NotNull(rows[2].GetString());
			Assert.Equal(10, rows[2].GetString()!.Length);
		}
	}
}