using System;
using System.IO;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameSaveToolHandlerNoGameTests : IDisposable
	{
		private readonly string _tempFolder;
		private readonly MockRuntime _runtime;
		private readonly GameSaveToolHandler _testee;

		public GameSaveToolHandlerNoGameTests()
		{
			_tempFolder = Path.Combine(Path.GetTempPath(), $"civone-mcp-save-nogame-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempFolder);

			RuntimeSettings runtimeSettings = new();
			runtimeSettings["mcp-saves"] = _tempFolder;

			_runtime = new MockRuntime(runtimeSettings);
			_testee = new GameSaveToolHandler(_runtime, new JsonSaveGameStateWriter(), 32000);
		}

		[Fact]
		public void HandleInvalidParamsReturnsInvalidParamsError()
		{
			using JsonDocument args = JsonDocument.Parse("[]");
			McpRequest request = new("2.0", "save-nogame-1", "game_save", args.RootElement.Clone(), null);

			McpResponse response = _testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.False(payload.RootElement.GetProperty("ok").GetBoolean());
			Assert.Equal("INVALID_PARAMS", payload.RootElement.GetProperty("error").GetProperty("code").GetString());
		}

		[Fact]
		public void HandleGameNotStartedReturnsGameNotStartedError()
		{
			using JsonDocument args = JsonDocument.Parse("{}");
			McpRequest request = new("2.0", "save-nogame-2", "game_save", args.RootElement.Clone(), null);

			McpResponse response = _testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.False(payload.RootElement.GetProperty("ok").GetBoolean());
			Assert.Equal("GAME_NOT_STARTED", payload.RootElement.GetProperty("error").GetProperty("code").GetString());
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

			_runtime.Dispose();
			RuntimeHandler.Wipe();
			Game.Wipe();
		}
	}
}
