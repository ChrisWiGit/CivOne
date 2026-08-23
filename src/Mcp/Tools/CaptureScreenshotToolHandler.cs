using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivOne.Mcp.Automation;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Tools
{
	public sealed class CaptureScreenshotToolHandler : IMcpToolHandler
	{
		private readonly IMcpScreenshotRoutine _screenshotRoutine;

		public string Method => "game_capture_screenshot";

		public ToolDefinition Definition => new(
			"game_capture_screenshot",
			"Captures a full-frame PNG screenshot of the current game state.",
			new
			{
				type = "object",
				properties = new
				{
					sessionId = new { type = "string", description = "Artifact session identifier." },
					includeCursor = new { type = "boolean", description = "Include cursor overlay (reserved)." }
				}
			});

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			string sessionId = ReadString(request.Params, "sessionId") ?? "default";
			bool includeCursor = ReadBoolean(request.Params, "includeCursor");
			McpScreenshotResult result = _screenshotRoutine.CaptureFull(sessionId, includeCursor);
			string json = JsonSerializer.Serialize(result, JsonOptions);
			return McpResponse.Success(request.Id, McpToolCallResult.Text(json));
		}
		private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		private static string? ReadString(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property)
				? property.GetString()
				: null;

		private static bool ReadBoolean(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property) &&
			   property.ValueKind == JsonValueKind.True;

		public CaptureScreenshotToolHandler(IMcpScreenshotRoutine screenshotRoutine)
		{
			_screenshotRoutine = screenshotRoutine ?? throw new ArgumentNullException(nameof(screenshotRoutine));
		}
	}
}
