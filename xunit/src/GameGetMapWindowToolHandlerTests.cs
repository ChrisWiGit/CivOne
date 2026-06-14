using System;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameGetMapWindowToolHandlerTests
	{
		[Fact]
		public void HandleEncodedFormatIncludesMetaDocPathAndEncodedRows()
		{
			// Arrange
			GameGetMapWindowToolHandler testee = CreateTestee();

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":2,\"height\":1,\"format\":\"encoded\"}");
			McpRequest request = new("2.0", "test-1", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			string payloadText = Assert.Single(result.Content).Text;
			using JsonDocument payload = JsonDocument.Parse(payloadText);

			JsonElement root = payload.RootElement;
			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.Equal("encoded", root.GetProperty("data").GetProperty("format").GetString());

			JsonElement meta = root.GetProperty("meta");
			Assert.Equal("docs/YAML_TILE_ENCODING.md", meta.GetProperty("tileEncodingDocPath").GetString());
			Assert.False(meta.GetProperty("landValuesIncluded").GetBoolean());

			JsonElement rows = root.GetProperty("data").GetProperty("window").GetProperty("rows");
			Assert.Equal(1, rows.GetArrayLength());
			Assert.NotNull(rows[0].GetString());
			Assert.Equal(4, rows[0].GetString()!.Length); // 2 tiles * 2 chars per tile
		}

		[Fact]
		public void HandleEncodedFormatIncludeMetaFalseStillReturnsMeta()
		{
			// Arrange
			GameGetMapWindowToolHandler testee = CreateTestee();

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"format\":\"encoded\",\"includeMeta\":false}");
			McpRequest request = new("2.0", "test-2", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);
			JsonElement root = payload.RootElement;

			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.True(root.TryGetProperty("meta", out JsonElement meta));
			Assert.Equal("encoded", meta.GetProperty("format").GetString());
		}

		[Fact]
		public void HandleDecodedFormatWithIncludeMetaTrueReturnsDecodedMeta()
		{
			// Arrange
			GameGetMapWindowToolHandler testee = CreateTestee();

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":2,\"height\":2,\"format\":\"decoded\",\"includeMeta\":true}");
			McpRequest request = new("2.0", "test-3", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);
			JsonElement root = payload.RootElement;

			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.Equal("decoded", root.GetProperty("data").GetProperty("format").GetString());

			JsonElement meta = root.GetProperty("meta");
			Assert.Equal("decoded", meta.GetProperty("format").GetString());
			Assert.Equal(4, meta.GetProperty("tileCount").GetInt32());
			Assert.False(meta.GetProperty("landValuesIncluded").GetBoolean());
		}

		[Fact]
		public void HandleInvalidBoundsReturnsInvalidBoundsError()
		{
			// Arrange
			GameGetMapWindowToolHandler testee = CreateTestee();

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":1,\"y\":0,\"width\":2,\"height\":1}");
			McpRequest request = new("2.0", "test-4", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);
			JsonElement root = payload.RootElement;

			Assert.False(root.GetProperty("ok").GetBoolean());
			Assert.Equal("INVALID_BOUNDS", root.GetProperty("error").GetProperty("code").GetString());
		}

		[Fact]
		public void HandleInvalidFormatReturnsInvalidFormatError()
		{
			// Arrange
			GameGetMapWindowToolHandler testee = CreateTestee();

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"format\":\"binary\"}");
			McpRequest request = new("2.0", "test-5", "game_get_map_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);
			JsonElement root = payload.RootElement;

			Assert.False(root.GetProperty("ok").GetBoolean());
			Assert.Equal("INVALID_FORMAT", root.GetProperty("error").GetProperty("code").GetString());
		}

		private static GameGetMapWindowToolHandler CreateTestee()
		{
			GameStateDto snapshot = new()
			{
				Map = new MapDto
				{
					Tiles = new Map2d<TileDto>(new[,]
					{
						{ new TileDto { Terrain = Enums.Terrain.Grassland1 }, new TileDto { Terrain = Enums.Terrain.Plains } },
						{ new TileDto { Terrain = Enums.Terrain.Ocean }, new TileDto { Terrain = Enums.Terrain.Hills } }
					})
				},
				Players = []
			};

			IGameStateDtoSnapshotProvider snapshotProvider = new StaticSnapshotProvider(snapshot);
			return new GameGetMapWindowToolHandler(snapshotProvider, new JsonSaveGameStateWriter(), 32000);
		}

		private sealed class StaticSnapshotProvider(GameStateDto snapshot) : IGameStateDtoSnapshotProvider
		{
			public bool TryGetSnapshot(out GameStateDto? output, out string? errorCode, out string? errorMessage)
			{
				output = snapshot;
				errorCode = null;
				errorMessage = null;
				return true;
			}
		}
	}
}