using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetPlayersToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_players";

		public ToolDefinition Definition => new(
			"game_get_players",
			"Returns players data. Optionally filter by playerId and/or selected keys.",
			new
			{
				type = "object",
				properties = new
				{
					playerId = new { type = "integer", description = "Optional player id (0-based)." },
					keys = new
					{
						type = "array",
						items = new { type = "string" },
						description = "Optional list of keys to include. If omitted, all keys are returned."
					}
				}
			});

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (!ValidateParamsObject(request, out McpResponse? validationError))
				return validationError!;

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null));

			if (!TryReadOptionalInt(request.Params, "playerId", out int? playerId, out string? playerIdError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerIdError, "playerId"));

			if (!TryReadKeys(request.Params, out string[] keys, out string? keysError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", keysError, "keys"));

			List<PlayerDto> players = snapshot!.Players ?? [];
			IEnumerable<PlayerDto> selectedPlayers = players;

			if (playerId.HasValue)
			{
				if (playerId.Value < 0 || playerId.Value >= players.Count)
					return JsonResponse(request.Id, BuildErrorPayload("INVALID_PLAYER_ID", "Player id is out of range.", "playerId"));

				selectedPlayers = [players[playerId.Value]];
			}

			if (keys.Length == 0)
				return JsonResponse(request.Id, BuildSuccessPayload(selectedPlayers));

			List<object> projected = [];
			HashSet<string> invalidKeys = new(StringComparer.OrdinalIgnoreCase);
			foreach (PlayerDto player in selectedPlayers)
			{
				projected.Add(ProjectByKeys(player, keys, invalidKeys));
			}

			if (invalidKeys.Count > 0)
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_KEYS",
					$"Unknown keys: {string.Join(", ", invalidKeys.OrderBy(x => x))}",
					"keys"));
			}

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

				output[propertyMap.Keys.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase))] = 
					JsonSerializer.Deserialize<object>(value.GetRawText()) ?? null !;
			}

			return output;
		}

		private McpResponse JsonResponse(object? id, object payload)
		{
			string json = _jsonWriter.AsString(payload);
			if (json.Length > _maxJsonChars)
				json = BuildTruncatedPayload(json);

			return McpResponse.Success(id, McpToolCallResult.Text(json));
		}

		private string BuildTruncatedPayload(string sourceJson)
		{
			const int reserveChars = 512;
			int previewChars = Math.Max(0, Math.Min(sourceJson.Length, _maxJsonChars - reserveChars));
			string preview = sourceJson.Substring(0, previewChars);

			while (preview.Length > 0)
			{
				string candidate = _jsonWriter.AsString(new
				{
					ok = false,
					truncated = true,
					maxChars = _maxJsonChars,
					returnedChars = preview.Length,
					totalChars = sourceJson.Length,
					strategy = "head-preview",
					dataPreview = preview,
					error = new { code = "PAYLOAD_TRUNCATED", message = "The payload exceeded the configured size limit and was truncated." }
				});

				if (candidate.Length <= _maxJsonChars)
					return candidate;

				preview = preview.Substring(0, Math.Max(0, preview.Length - Math.Min(256, preview.Length)));
			}

			return _jsonWriter.AsString(new
			{
				ok = false,
				truncated = true,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				totalChars = sourceJson.Length,
				error = new { code = "PAYLOAD_TRUNCATED", message = "The payload exceeded the configured size limit and was truncated." }
			});
		}

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
				error = new
				{
					code,
					message,
					path = (string)null!,
					failedSegment
				}
			};
		}

		private static bool ValidateParamsObject(McpRequest request, [NotNullWhen(false)] out McpResponse? response)
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

		private static bool TryReadOptionalInt(JsonElement value, string propertyName, out int? result, [NotNullWhen(false)] out string? error)
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

		private static bool TryReadKeys(JsonElement value, out string[] keys, [NotNullWhen(false)] out string? error)
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

		public GameGetPlayersToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars)
		{
			_snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}
	}
}
