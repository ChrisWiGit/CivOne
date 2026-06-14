using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameGetMapLandValuesWindowToolHandlerTests
	{
		[Fact]
		public void HandleValidWindowReturnsHexLandValueRows()
		{
			// Arrange
			GameStateDto snapshot = new()
			{
				Map = new MapDto
				{
					Tiles = new Map2d<TileDto>(new[,]
					{
						{ new TileDto { LandValue = 1 }, new TileDto { LandValue = 2 } },
						{ new TileDto { LandValue = 10 }, new TileDto { LandValue = 255 } }
					})
				},
				Players = []
			};

			IGameStateDtoSnapshotProvider snapshotProvider = new StaticSnapshotProvider(snapshot);
			GameGetMapLandValuesWindowToolHandler testee = new(snapshotProvider, new JsonSaveGameStateWriter(), 32000);

			using JsonDocument argsDoc = JsonDocument.Parse("{\"x\":0,\"y\":0,\"width\":2,\"height\":2}");
			McpRequest request = new("2.0", "lv-1", "game_get_map_landvalues_window", argsDoc.RootElement.Clone(), null);

			// Act
			McpResponse response = testee.Handle(request);

			// Assert
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			JsonElement root = payload.RootElement;
			Assert.True(root.GetProperty("ok").GetBoolean());
			Assert.Equal("hex-csv-per-row", root.GetProperty("data").GetProperty("landValuesFormat").GetString());

			JsonElement rows = root.GetProperty("data").GetProperty("rows");
			Assert.Equal(2, rows.GetArrayLength());
			Assert.Equal("01,0A", rows[0].GetString());
			Assert.Equal("02,FF", rows[1].GetString());
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