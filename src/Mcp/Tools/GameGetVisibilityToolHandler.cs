using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetVisibilityToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_visibility";

		public ToolDefinition Definition => new(
			"game_get_visibility",
			"Returns explored/visible masks for a player. Optional bounding box can be supplied.",
			new
			{
				type = "object",
				properties = new
				{
					playerId = new { type = "integer", description = "Player id (0-based). Required." },
					x = new { type = "integer", description = "Optional bounding-box x coordinate." },
					y = new { type = "integer", description = "Optional bounding-box y coordinate." },
					width = new { type = "integer", description = "Optional bounding-box width." },
					height = new { type = "integer", description = "Optional bounding-box height." }
				}
			});

		public GameGetVisibilityToolHandler(
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

			if (!TryReadInt(request.Params, "playerId", out int playerId, out string? playerIdError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerIdError, "playerId"));

			if (!TryReadOptionalInt(request.Params, "x", out int? x, out string? xError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", xError, "x"));

			if (!TryReadOptionalInt(request.Params, "y", out int? y, out string? yError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", yError, "y"));

			if (!TryReadOptionalInt(request.Params, "width", out int? width, out string? widthError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", widthError, "width"));

			if (!TryReadOptionalInt(request.Params, "height", out int? height, out string? heightError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", heightError, "height"));

			bool anyBoundsProvided = x.HasValue || y.HasValue || width.HasValue || height.HasValue;
			if (anyBoundsProvided && !(x.HasValue && y.HasValue && width.HasValue && height.HasValue))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", "If any bounding-box field is provided, x, y, width, and height are all required.", "x|y|width|height"));

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null));

			List<PlayerDto> players = snapshot!.Players ?? [];
			if (playerId < 0 || playerId >= players.Count)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PLAYER_ID", "Player id is out of range.", "playerId"));

			Map2d<TileDto>? mapTiles = snapshot.Map?.Tiles;
			if (mapTiles == null)
				return JsonResponse(request.Id, BuildErrorPayload("NO_MAP", "No map data available.", null));

			int mapWidth = mapTiles.Width();
			int mapHeight = mapTiles.Height();

			int bx = x ?? 0;
			int by = y ?? 0;
			int bw = width ?? mapWidth;
			int bh = height ?? mapHeight;

			if (!ValidateBounds(bx, by, bw, bh, mapWidth, mapHeight, out string? boundsError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_BOUNDS", boundsError!, "x|y|width|height"));

			PlayerDto player = players[playerId];
			Bool2dMap explored = player.Explored;
			Bool2dMap visible = player.Visible;

			if (explored == null || visible == null)
				return JsonResponse(request.Id, BuildErrorPayload("NO_VISIBILITY", "Visibility data is not available for the selected player.", "playerId"));

			string[] exploredRows = ReadRows(explored, bx, by, bw, bh, out int exploredCount);
			string[] visibleRows = ReadRows(visible, bx, by, bw, bh, out int visibleCount);

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				playerId,
				mapSize = new { width = mapWidth, height = mapHeight },
				bounds = new { x = bx, y = by, width = bw, height = bh },
				exploredCount,
				visibleCount,
				exploredRows,
				visibleRows
			}));
		}

		private static string[] ReadRows(Bool2dMap map, int x, int y, int width, int height, out int trueCount)
		{
			trueCount = 0;
			string[] rows = new string[height];
			for (int row = 0; row < height; row++)
			{
				StringBuilder buffer = new(width);
				for (int col = 0; col < width; col++)
				{
					bool value = map[x + col, y + row];
					if (value)
						trueCount++;
					buffer.Append(value ? '1' : '0');
				}

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

		private static bool TryReadInt(JsonElement value, string propertyName, out int result, out string? error)
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
	}
}