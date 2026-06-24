using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Persistence;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class GameSaveToolHandlerTests : TestsBase
	{
		private readonly string _tempFolder;
		private readonly MockRuntime _runtime;

		public GameSaveToolHandlerTests()
		{
			_tempFolder = Path.Combine(Path.GetTempPath(), $"civone-mcp-save-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempFolder);

			RuntimeSettings runtimeSettings = new();
			runtimeSettings["mcp-saves"] = _tempFolder;
			_runtime = new MockRuntime(runtimeSettings);
		}

		[Fact]
		public void HandleWithActiveGameCreatesNewCosFileAndReturnsFileNameAndGuid()
		{
			DateTimeOffset fixedNow = new(2026, 4, 30, 18, 45, 12, 123, TimeSpan.Zero);
			GameSaveToolHandler testee = new(_runtime, new JsonSaveGameStateWriter(), 32000, () => fixedNow);
			using JsonDocument args = JsonDocument.Parse("{}");
			McpRequest request = new("2.0", "save-1", "game_save", args.RootElement.Clone(), null);

			McpResponse response = testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.True(payload.RootElement.GetProperty("ok").GetBoolean());
			JsonElement data = payload.RootElement.GetProperty("data");
			string? fileName = data.GetProperty("fileName").GetString();
			string? saveGuidText = data.GetProperty("saveGuid").GetString();

			Assert.NotNull(fileName);
			Assert.NotNull(saveGuidText);
			Assert.Equal("savegame_mcp_20260430184512123.cos", fileName);
			Assert.True(Guid.TryParse(saveGuidText, out Guid _));
			Assert.True(File.Exists(Path.Combine(_tempFolder, fileName)));
		}

		[Fact]
		public void HandleWhenTimestampFileAlreadyExistsReturnsFileExistsError()
		{
			DateTimeOffset fixedNow = new(2026, 4, 30, 18, 45, 12, 123, TimeSpan.Zero);
			string existingFile = Path.Combine(_tempFolder, "savegame_mcp_20260430184512123.cos");
			File.WriteAllText(existingFile, "already there");
			GameSaveToolHandler testee = new(_runtime, new JsonSaveGameStateWriter(), 32000, () => fixedNow);
			using JsonDocument args = JsonDocument.Parse("{}");
			McpRequest request = new("2.0", "save-2", "game_save", args.RootElement.Clone(), null);

			McpResponse response = testee.Handle(request);
			McpToolCallResult result = Assert.IsType<McpToolCallResult>(response.Result);
			using JsonDocument payload = JsonDocument.Parse(Assert.Single(result.Content).Text);

			Assert.False(payload.RootElement.GetProperty("ok").GetBoolean());
			Assert.Equal("FILE_EXISTS", payload.RootElement.GetProperty("error").GetProperty("code").GetString());
			Assert.Contains("wait a second", payload.RootElement.GetProperty("error").GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
		}

		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test cleanup should not throw exceptions")]
		protected override void AfterEach()
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
		}
	}
}
