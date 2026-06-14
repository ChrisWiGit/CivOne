using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetPlayerToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_player";

		public ToolDefinition Definition => new(
			"game_get_player",
			"Returns one player by playerId (0-based index) or playerGuid, with optional key filtering.",
			new
			{
				type = "object",
				properties = new
				{
					playerId = new { type = "integer", description = "Optional player index (0-based)." },
					playerGuid = new { type = "string", description = "Optional stable player GUID." },
					keys = new
					{
						type = "array",
						items = new { type = "string" },
						description = "Optional list of keys to include. If omitted, all keys are returned."
					}
				}
			});

		public GameGetPlayerToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars)
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

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null));

			if (!TryReadOptionalInt(request.Params, "playerId", out int? playerId, out string? playerIdError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerIdError, "playerId"));

			if (!TryReadOptionalGuid(request.Params, "playerGuid", out Guid? playerGuid, out string? playerGuidError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerGuidError, "playerGuid"));

			if (!TryReadKeys(request.Params, out string[] keys, out string? keysError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", keysError!, "keys"));

			if (!playerId.HasValue && !playerGuid.HasValue)
				return JsonResponse(request.Id, BuildErrorPayload("MISSING_SELECTOR", "Provide either 'playerId' or 'playerGuid'.", "playerId|playerGuid"));

			if (playerId.HasValue && playerGuid.HasValue)
				return JsonResponse(request.Id, BuildErrorPayload("AMBIGUOUS_SELECTOR", "Provide only one selector: 'playerId' or 'playerGuid'.", "playerId|playerGuid"));

			List<PlayerDto> players = snapshot!.Players ?? [];
			PlayerDto? player = null;

			if (playerId.HasValue)
			{
				if (playerId.Value < 0 || playerId.Value >= players.Count)
					return JsonResponse(request.Id, BuildErrorPayload("INVALID_PLAYER_ID", "Player id is out of range.", "playerId"));

				player = players[playerId.Value];
			}

			if (playerGuid.HasValue)
				player = players.FirstOrDefault(x => x.PlayerGuid == playerGuid.Value);

			if (player == null)
				return JsonResponse(request.Id, BuildErrorPayload("PLAYER_NOT_FOUND", "No player found for the specified selector.", playerGuid.HasValue ? "playerGuid" : "playerId"));

			if (keys.Length == 0)
				return JsonResponse(request.Id, BuildSuccessPayload(player));

			HashSet<string> invalidKeys = new(StringComparer.OrdinalIgnoreCase);
			object projected = ProjectByKeys(player, keys, invalidKeys);

			if (invalidKeys.Count > 0)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_KEYS", $"Unknown keys: {string.Join(", ", invalidKeys.OrderBy(x => x))}", "keys"));

			return JsonResponse(request.Id, BuildSuccessPayload(projected));
		}

		private Dictionary<string, object> ProjectByKeys(object source, string[] keys, HashSet<string> invalidKeys)
		{
			using JsonDocument document = JsonDocument.Parse(_jsonWriter.AsString(source));
			Dictionary<string, JsonElement> propertyMap = document.RootElement
				.EnumerateObject()
				.ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

			Dictionary<string, object> output = new(StringComparer.OrdinalIgnoreCase);
			foreach (string key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!propertyMap.TryGetValue(key, out JsonElement value))
				{
					invalidKeys.Add(key);
					continue;
				}

				output[propertyMap.Keys.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase))] = JsonSerializer.Deserialize<object>(value.GetRawText()) ?? null!;
			}

			return output;
		}

		private McpResponse JsonResponse(object? id, object payload)
			=> McpJsonToolResponse.JsonResponse(id, payload, _jsonWriter, _maxJsonChars);

		private object BuildSuccessPayload(object data)
		{
			string payloadJson = _jsonWriter.AsString(data ?? new { });
			return new
			{
				ok = true,
				path = (string)null!,
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
				path = (string)null!,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new { code, message, path = (string)null!, failedSegment }
			};
		}

		private static bool ValidateParamsObject(McpRequest request, out McpResponse? response)
		{
			response = null;
			if (request.Params.ValueKind == JsonValueKind.Undefined || request.Params.ValueKind == JsonValueKind.Null)
				return true;

			if (request.Params.ValueKind == JsonValueKind.Object)
				return true;

			response = McpResponse.Success(request.Id, McpToolCallResult.Text(JsonSerializer.Serialize(new
			{
				ok = false,
				error = new { code = "INVALID_PARAMS", message = "'params' must be an object." }
			})));
			return false;
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

		private static bool TryReadOptionalGuid(JsonElement value, string propertyName, out Guid? result, out string? error)
		{
			result = null;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.String || !Guid.TryParse(property.GetString(), out Guid parsed))
			{
				error = $"Property '{propertyName}' must be a GUID string.";
				return false;
			}

			result = parsed;
			return true;
		}

		private static bool TryReadKeys(JsonElement value, out string[] keys, out string? error)
		{
			keys = [];
			error = null;

			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("keys", out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.Array)
			{
				error = "Property 'keys' must be an array of strings.";
				return false;
			}

			List<string> parsed = [];
			foreach (JsonElement item in property.EnumerateArray())
			{
				if (item.ValueKind != JsonValueKind.String)
				{
					error = "Property 'keys' must contain only strings.";
					return false;
				}

				string? text = item.GetString();
				if (!string.IsNullOrWhiteSpace(text))
					parsed.Add(text.Trim());
			}

			keys = [.. parsed];
			return true;
		}
	}
}