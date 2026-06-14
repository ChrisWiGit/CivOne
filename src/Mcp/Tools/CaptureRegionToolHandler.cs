using System;
using System.Text.Json;
using CivOne.Mcp.Automation;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Tools
{
	public sealed class CaptureRegionToolHandler : IMcpToolHandler
	{
		private readonly IMcpScreenshotRoutine _screenshotRoutine;

		public string Method => "game_capture_region";

		public ToolDefinition Definition => new(
			"game_capture_region",
			"Captures a rectangular region of the current game frame as a PNG screenshot.",
			new
			{
				type = "object",
				required = InputSchema,
				properties = new
				{
					sessionId = new { type = "string", description = "Artifact session identifier." },
					x = new { type = "integer", description = "Left edge of the region in pixels." },
					y = new { type = "integer", description = "Top edge of the region in pixels." },
					width = new { type = "integer", description = "Width of the region in pixels (>0)." },
					height = new { type = "integer", description = "Height of the region in pixels (>0)." },
					includeCursor = new { type = "boolean", description = "Include cursor overlay (reserved)." }
				}
			});

		private static readonly string[] InputSchema = new[] { "x", "y", "width", "height" };

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			string sessionId = ReadString(request.Params, "sessionId") ?? "default";
			int x = ReadInt(request.Params, "x");
			int y = ReadInt(request.Params, "y");
			int width = ReadInt(request.Params, "width");
			int height = ReadInt(request.Params, "height");
			bool includeCursor = ReadBoolean(request.Params, "includeCursor");

			if (width <= 0 || height <= 0)
				return McpResponse.Failure(request.Id, -32602, "Invalid params", "Region width and height must be greater than zero.");

			McpScreenshotResult result = _screenshotRoutine.CaptureRegion(sessionId, x, y, width, height, includeCursor);
			string json = JsonSerializer.Serialize(result, StandardSerializerOptions);
			return McpResponse.Success(request.Id, McpToolCallResult.Text(json));
		}

		private static readonly JsonSerializerOptions StandardSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		private static string? ReadString(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property)
				? property.GetString()
				: null;

		private static int ReadInt(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property) &&
				property.ValueKind == JsonValueKind.Number
				? property.GetInt32()
				: 0;

		private static bool ReadBoolean(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property) &&
				property.ValueKind == JsonValueKind.True;

		public CaptureRegionToolHandler(IMcpScreenshotRoutine screenshotRoutine)
		{
			_screenshotRoutine = screenshotRoutine ?? throw new ArgumentNullException(nameof(screenshotRoutine));
		}
	}
}
