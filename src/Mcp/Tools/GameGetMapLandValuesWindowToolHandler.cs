using System;
using System.Globalization;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetMapLandValuesWindowToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_map_landvalues_window";

		public ToolDefinition Definition => new(
			"game_get_map_landvalues_window",
			"Returns bounded map land values (city-site heuristic values) for x, y, width, and height.",
			new
			{
				type = "object",
				properties = new
				{
					x = new { type = "integer", description = "Window origin x (required)." },
					y = new { type = "integer", description = "Window origin y (required)." },
					width = new { type = "integer", description = "Window width (required, > 0)." },
					height = new { type = "integer", description = "Window height (required, > 0)." }
				}
			});

		public GameGetMapLandValuesWindowToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars = GameGetStateToolHandler.MaxJsonCharsDefault)
		{
			_snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (!ValidateParamsObject(request, out McpResponse? validationError))
				return validationError!;

			if (!TryReadRequiredInt(request.Params, "x", out int x, out string? xError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", xError, "x"));

			if (!TryReadRequiredInt(request.Params, "y", out int y, out string? yError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", yError, "y"));

			if (!TryReadRequiredInt(request.Params, "width", out int width, out string? widthError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", widthError, "width"));

			if (!TryReadRequiredInt(request.Params, "height", out int height, out string? heightError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", heightError, "height"));

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null));

			Map2d<TileDto>? mapTiles = snapshot!.Map?.Tiles;
			if (mapTiles == null)
				return JsonResponse(request.Id, BuildErrorPayload("NO_MAP", "No map data available.", null));

			int mapWidth = mapTiles.Width();
			int mapHeight = mapTiles.Height();

			if (!ValidateBounds(x, y, width, height, mapWidth, mapHeight, out string? boundsError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_BOUNDS", boundsError!, "x|y|width|height"));

			string[] rows = new string[height];
			for (int row = 0; row < height; row++)
			{
				string[] values = new string[width];
				for (int col = 0; col < width; col++)
				{
					byte landValue = mapTiles[x + col, y + row].LandValue;
					values[col] = landValue.ToString("X2", CultureInfo.InvariantCulture);
				}

				rows[row] = string.Join(',', values);
			}

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				mapSize = new { width = mapWidth, height = mapHeight },
				bounds = new { x, y, width, height },
				landValuesFormat = "hex-csv-per-row",
				rows
			}));
		}

		private McpResponse JsonResponse(object? id, object payload)
			=> McpJsonToolResponse.JsonResponse(id, payload, _jsonWriter, _maxJsonChars);

		private object BuildSuccessPayload(object data)
		{
			string payloadJson = _jsonWriter.AsString(data ?? new { });
			return new
			{
				ok = true,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = payloadJson.Length,
				data = data ?? new { }
			};
		}

		private object BuildErrorPayload(string code, string? message, string? failedSegment)
		{
			return new
			{
				ok = false,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new
				{
					code,
					message,
					path = (string)null!,
					failedSegment
				}
			};
		}

		private static bool ValidateBounds(int x, int y, int width, int height, int mapWidth, int mapHeight, out string? error)
		{
			error = null;
			if (x < 0 || y < 0)
			{
				error = "x and y must be >= 0.";
				return false;
			}

			if (width <= 0 || height <= 0)
			{
				error = "width and height must be > 0.";
				return false;
			}

			if (x + width > mapWidth || y + height > mapHeight)
			{
				error = $"Bounds exceed map size ({mapWidth}x{mapHeight}).";
				return false;
			}

			return true;
		}

		private static bool ValidateParamsObject(McpRequest request, out McpResponse? response)
		{
			response = null;
			if (request.Params.ValueKind == JsonValueKind.Object)
				return true;

			response = McpResponse.Success(request.Id, McpToolCallResult.Text(JsonSerializer.Serialize(new
			{
				ok = false,
				error = new { code = "INVALID_PARAMS", message = "'params' must be an object." }
			})));
			return false;
		}

		private static bool TryReadRequiredInt(JsonElement value, string propertyName, out int result, out string? error)
		{
			result = 0;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out JsonElement property))
			{
				error = $"Property '{propertyName}' is required.";
				return false;
			}

			if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out int parsed))
			{
				error = $"Property '{propertyName}' must be an integer.";
				return false;
			}

			result = parsed;
			return true;
		}
	}
}