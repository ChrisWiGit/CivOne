using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	#pragma warning disable CA1822 // Mark members as static
	public sealed class GameGetMapWindowToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;
		private readonly TileCodec _tileCodec;

		public string Method => "game_get_map_window";

		public ToolDefinition Definition => new(
			"game_get_map_window",
			"Returns a bounded map window. Requires x, y, width, and height. Optional player visibility context and decoded/encoded formats are supported.",
			new
			{
				type = "object",
				properties = new
				{
					x = new { type = "integer", description = "Window origin x (required)." },
					y = new { type = "integer", description = "Window origin y (required)." },
					width = new { type = "integer", description = "Window width (required, > 0)." },
					height = new { type = "integer", description = "Window height (required, > 0)." },
					visibilityForPlayerId = new { type = "integer", description = "Optional player id (0-based) to include explored/visible overlays." },
					format = new { type = "string", description = "Optional format: 'decoded' (default) or 'encoded'." },
					includeMeta = new { type = "boolean", description = "Optional metadata toggle. For 'encoded' format, metadata is always included." }
				}
			});

		public GameGetMapWindowToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars = GameGetStateToolHandler.MaxJsonCharsDefault)
		{
			_snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_maxJsonChars = Math.Max(512, maxJsonChars);
			_tileCodec = new TileCodec();
		}

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (!ValidateParamsObject(request, out McpResponse? validationError))
				return validationError!;

			if (!TryReadRequiredInt(request.Params, "x", out int x, out string? xError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", xError ?? "should not happen", "x"));

			if (!TryReadRequiredInt(request.Params, "y", out int y, out string? yError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", yError ?? "should not happen", "y"));

			if (!TryReadRequiredInt(request.Params, "width", out int width, out string? widthError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", widthError ?? "should not happen", "width"));

			if (!TryReadRequiredInt(request.Params, "height", out int height, out string? heightError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", heightError ?? "should not happen", "height"));

			if (!TryReadOptionalInt(request.Params, "visibilityForPlayerId", out int? visibilityForPlayerId, out string? playerError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerError ?? "should not happen", "visibilityForPlayerId"));

			if (!TryReadOptionalString(request.Params, "format", out string? format, out string? formatError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", formatError ?? "should not happen", "format"));

			if (!TryReadOptionalBool(request.Params, "includeMeta", out bool includeMeta, out string? includeMetaError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", includeMetaError ?? "should not happen", "includeMeta"));

			if (string.IsNullOrWhiteSpace(format))
				format = "decoded";

			if (!format.Equals("decoded", StringComparison.OrdinalIgnoreCase) && !format.Equals("encoded", StringComparison.OrdinalIgnoreCase))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_FORMAT", "Property 'format' must be either 'decoded' or 'encoded'.", "format"));

			bool encoded = format.Equals("encoded", StringComparison.OrdinalIgnoreCase);
			if (encoded)
				includeMeta = true;

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto snapshot, out string errorCode, out string errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode, errorMessage, null));

			Map2d<TileDto>? mapTiles = snapshot.Map?.Tiles;
			if (mapTiles == null)
				return JsonResponse(request.Id, BuildErrorPayload("NO_MAP", "No map data available.", null));

			int mapWidth = mapTiles.Width();
			int mapHeight = mapTiles.Height();

			if (!ValidateBounds(x, y, width, height, mapWidth, mapHeight, out string? boundsError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_BOUNDS", boundsError ?? "should not happen", "x|y|width|height"));

			Bool2dMap? explored = null;
			Bool2dMap? visible = null;
			if (visibilityForPlayerId.HasValue)
			{
				List<PlayerDto> players = snapshot.Players ?? [];
				if (visibilityForPlayerId.Value < 0 || visibilityForPlayerId.Value >= players.Count)
					return JsonResponse(request.Id, BuildErrorPayload("INVALID_PLAYER_ID", "Player id is out of range.", "visibilityForPlayerId"));

				PlayerDto player = players[visibilityForPlayerId.Value];
				explored = player.Explored;
				visible = player.Visible;
			}

			object windowData = encoded
				? BuildEncodedRows(mapTiles, x, y, width, height)
				: BuildDecodedRows(mapTiles, x, y, width, height, explored, visible);

			object? visibilityData = null;
			if (visibilityForPlayerId.HasValue && explored != null && visible != null)
			{
				visibilityData = new
				{
					playerId = visibilityForPlayerId.Value,
					exploredRows = BuildBoolRows(explored, x, y, width, height),
					visibleRows = BuildBoolRows(visible, x, y, width, height)
				};
			}

			Dictionary<string, object> responseData = new(StringComparer.OrdinalIgnoreCase)
			{
				["mapSize"] = new { width = mapWidth, height = mapHeight },
				["bounds"] = new { x, y, width, height },
				["format"] = encoded ? "encoded" : "decoded",
				["window"] = windowData
			};

			if (visibilityData != null)
				responseData["visibility"] = visibilityData;

			object? meta = null;
			if (includeMeta)
			{
				meta = encoded
					? new
					{
						schemaVersion = "1.0",
						format = "encoded",
						strictBounds = true,
						tileEncodingDocPath = "docs/YAML_TILE_ENCODING.md",
						landValuesIncluded = false,
						notes = new[] { "landValues are not included in this response." }
					}
					: new
					{
						schemaVersion = "1.0",
						format = "decoded",
						strictBounds = true,
						tileCount = width * height,
						landValuesIncluded = false
					};
			}

			return JsonResponse(request.Id, BuildSuccessPayload(responseData, meta));
		}

		private object BuildEncodedRows(Map2d<TileDto> mapTiles, int x, int y, int width, int height)
		{
			string[] rows = new string[height];
			for (int row = 0; row < height; row++)
			{
				StringBuilder builder = new(width * 2);
				for (int col = 0; col < width; col++)
				{
					TileDto tile = mapTiles[x + col, y + row];
					builder.Append(_tileCodec.Encode(tile));
				}

				rows[row] = builder.ToString();
			}

			return new { rows };
		}

		private object BuildDecodedRows(Map2d<TileDto> mapTiles, int x, int y, int width, int height, Bool2dMap? explored, Bool2dMap? visible)
		{
			List<object[]> rows = [];
			for (int row = 0; row < height; row++)
			{
				object[] outputRow = new object[width];
				for (int col = 0; col < width; col++)
				{
					int tx = x + col;
					int ty = y + row;
					TileDto tile = mapTiles[tx, ty];

					bool? isExplored = explored?[tx, ty];
					bool? isVisible = visible?[tx, ty];

					outputRow[col] = new
					{
						x = tx,
						y = ty,
						terrain = tile.Terrain.ToString(),
						road = tile.Road,
						railRoad = tile.RailRoad,
						irrigation = tile.Irrigation,
						pollution = tile.Pollution,
						fortress = tile.Fortress,
						mine = tile.Mine,
						hut = tile.Hut,
						special = tile.Special,
						explored = isExplored,
						visible = isVisible
					};
				}

				rows.Add(outputRow);
			}

			return new { rows };
		}

		private static string[] BuildBoolRows(Bool2dMap map, int x, int y, int width, int height)
		{
			string[] rows = new string[height];
			for (int row = 0; row < height; row++)
			{
				StringBuilder buffer = new(width);
				for (int col = 0; col < width; col++)
					buffer.Append(map[x + col, y + row] ? '1' : '0');

				rows[row] = buffer.ToString();
			}

			return rows;
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

		private McpResponse JsonResponse(object id, object payload)
			=> McpJsonToolResponse.JsonResponse(id, payload, _jsonWriter, _maxJsonChars);

		private Dictionary<string, object> BuildSuccessPayload(object data, object? meta)
		{
			Dictionary<string, object> payload = new(StringComparer.OrdinalIgnoreCase)
			{
				["ok"] = true,
				["truncated"] = false,
				["maxChars"] = _maxJsonChars,
				["returnedChars"] = _jsonWriter.AsString(data ?? new { }).Length,
				["data"] = data ?? new { }
			};

			if (meta != null)
				payload["meta"] = meta;

			return payload;
		}

		private Dictionary<string, object> BuildErrorPayload(string code, string message, string? failedSegment)
		{
			return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["ok"] = false,
				["truncated"] = false,
				["maxChars"] = _maxJsonChars,
				["returnedChars"] = 0,
				["error"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
				{
					["code"] = code,
					["message"] = message,
					["path"] = null,
					["failedSegment"] = failedSegment
				}
			};
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

		private static bool TryReadOptionalInt(JsonElement value, string propertyName, out int? result, out string? error)
		{
			result = null;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out int parsed))
			{
				error = $"Property '{propertyName}' must be an integer.";
				return false;
			}

			result = parsed;
			return true;
		}

		private static bool TryReadOptionalString(JsonElement value, string propertyName, out string? result, out string? error)
		{
			result = null;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.String)
			{
				error = $"Property '{propertyName}' must be a string.";
				return false;
			}

			result = property.GetString();
			return true;
		}

		private static bool TryReadOptionalBool(JsonElement value, string propertyName, out bool result, out string? error)
		{
			result = false;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
			{
				error = $"Property '{propertyName}' must be a boolean.";
				return false;
			}

			result = property.GetBoolean();
			return true;
		}
	}
}