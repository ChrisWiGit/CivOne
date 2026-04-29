using System;
using System.IO;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameLoadToolHandlerTests : IDisposable
	{
		private readonly string _tempFolder;
		private readonly RuntimeSettings _runtimeSettings;
		private readonly MockRuntime _mockRuntime;
		private readonly GameLoadToolHandler _testee;

		public GameLoadToolHandlerTests()
		{
			_tempFolder = Path.Combine(Path.GetTempPath(), $"civone-mcp-load-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempFolder);

			_runtimeSettings = new RuntimeSettings();
			_runtimeSettings["mcp-saves"] = _tempFolder;
			_mockRuntime = new MockRuntime(_runtimeSettings);
			_testee = new GameLoadToolHandler(_mockRuntime, new JsonSaveGameStateWriter(), 32000);
		}

		[Fact]
		public void Handle_PathTraversalInFileName_ReturnsInvalidFileName()
		{
			using JsonDocument args = JsonDocument.Parse("{\"fileName\":\"..\\\\evil.cos\"}");
			McpRequest request = new("2.0", "load-1", "game_load", args.RootElement.Clone(), null);

			McpResponse response = _testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.False(payload.RootElement.GetProperty("ok").GetBoolean());
			Assert.Equal("INVALID_FILE_NAME", payload.RootElement.GetProperty("error").GetProperty("code").GetString());
		}

		[Fact]
		public void Handle_MissingFile_ReturnsFileNotFound()
		{
			using JsonDocument args = JsonDocument.Parse("{\"fileName\":\"missing.cos\"}");
			McpRequest request = new("2.0", "load-2", "game_load", args.RootElement.Clone(), null);

			McpResponse response = _testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.False(payload.RootElement.GetProperty("ok").GetBoolean());
			Assert.Equal("FILE_NOT_FOUND", payload.RootElement.GetProperty("error").GetProperty("code").GetString());
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
