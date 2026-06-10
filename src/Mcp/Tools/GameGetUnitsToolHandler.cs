using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetUnitsToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_units";

		public ToolDefinition Definition => new(
			"game_get_units",
			"Returns units with optional filters: playerId, playerGuid, className, locationRadius, and keys.",
			new
			{
				type = "object",
				properties = new
				{
					playerId = new { type = "integer", description = "Optional owner player id (0-based index)." },
					playerGuid = new { type = "string", description = "Optional stable owner player GUID." },
					className = new { type = "string", description = "Optional unit class name filter (case-insensitive exact match)." },
					locationRadius = new
					{
						type = "object",
						description = "Optional radius filter around a map position.",
						properties = new
						{
							x = new { type = "integer", minimum = 0 },
							y = new { type = "integer", minimum = 0 },
							radius = new { type = "number", minimum = 0 }
						}
					},
					keys = new
					{
						type = "array",
						items = new { type = "string" },
						description = "Optional list of keys to include. If omitted, all keys are returned."
					}
				}
			});

		public GameGetUnitsToolHandler(
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

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto snapshot, out string errorCode, out string errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode, errorMessage, null));

			if (!TryReadOptionalInt(request.Params, "playerId", out int? playerId, out string? playerIdError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerIdError!, "playerId"));

			if (!TryReadOptionalGuid(request.Params, "playerGuid", out Guid? playerGuid, out string? playerGuidError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", playerGuidError!, "playerGuid"));

			if (!TryReadOptionalString(request.Params, "className", out string? className, out string? classNameError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", classNameError!, "className"));

			if (!TryReadOptionalLocationRadius(request.Params, out LocationRadius? radiusFilter, out string? radiusError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", radiusError!, "locationRadius"));

			if (!TryReadKeys(request.Params, out string[] keys, out string? keysError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", keysError!, "keys"));

			List<PlayerDto> players = snapshot.Players ?? [];
			IEnumerable<(int Index, PlayerDto Player)> selectedPlayers = players.Select((player, index) => (index, player));

			if (playerId.HasValue)
			{
				if (playerId.Value < 0 || playerId.Value >= players.Count)
					return JsonResponse(request.Id, BuildErrorPayload("INVALID_PLAYER_ID", "Player id is out of range.", "playerId"));

				selectedPlayers = [(playerId.Value, players[playerId.Value])];
			}

			if (playerGuid.HasValue)
			{
				selectedPlayers = selectedPlayers.Where(x => x.Player.PlayerGuid == playerGuid.Value);
				if (!selectedPlayers.Any())
					return JsonResponse(request.Id, BuildErrorPayload("PLAYER_NOT_FOUND", "No player found for the specified playerGuid.", "playerGuid"));
			}

			IEnumerable<object> units = selectedPlayers.SelectMany(x =>
				(x.Player.Units ?? []).Select(unit => BuildUnitWithOwner(unit, x.Index, x.Player.PlayerGuid, x.Player.TribeName)));

			if (!string.IsNullOrWhiteSpace(className))
				units = units.Where(unit => MatchesClassName(unit, className));

			if (radiusFilter != null)
				units = units.Where(unit => IsWithinRadius(unit, radiusFilter));

			if (keys.Length == 0)
				return JsonResponse(request.Id, BuildSuccessPayload(units));

			List<object> projected = [];
			HashSet<string> invalidKeys = new(StringComparer.OrdinalIgnoreCase);
			foreach (object unit in units)
				projected.Add(ProjectByKeys(unit, keys, invalidKeys));

			if (invalidKeys.Count > 0)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_KEYS", $"Unknown keys: {string.Join(", ", invalidKeys.OrderBy(x => x))}", "keys"));

			return JsonResponse(request.Id, BuildSuccessPayload(projected));
		}

		private static object BuildUnitWithOwner(UnitDto unit, int playerIndex, Guid playerGuid, string tribeName)
		{
			return new
			{
				unit.ClassName,
				unit.Location,
				unit.Goto,
				unit.HomeCityGuid,
				unit.Busy,
				unit.Veteran,
				unit.Sentry,
				unit.FortifyActive,
				unit.Fortify,
				unit.FuelOrProgress,
				unit.Fuel,
				unit.WorkProgress,
				unit.Order,
				unit.MovesSkip,
				unit.MovesLeft,
				unit.PartMoves,
				unit.PlayerId,
				owner = playerIndex,
				ownerGuid = playerGuid,
				ownerTribeName = tribeName
			};
		}

		private bool MatchesClassName(object source, string className)
		{
			using JsonDocument doc = JsonDocument.Parse(_jsonWriter.AsString(source));
			if (!TryGetPropertyIgnoreCase(doc.RootElement, "ClassName", out JsonElement classNameElement))
				return false;

			return string.Equals(classNameElement.GetString(), className, StringComparison.OrdinalIgnoreCase);
		}

		private bool IsWithinRadius(object source, LocationRadius radius)
		{
			using JsonDocument doc = JsonDocument.Parse(_jsonWriter.AsString(source));
			if (!TryGetPropertyIgnoreCase(doc.RootElement, "Location", out JsonElement locationElement))
				return false;

			if (locationElement.ValueKind != JsonValueKind.Object)
				return false;

			if (!TryGetPropertyIgnoreCase(locationElement, "X", out JsonElement xElement) || xElement.ValueKind != JsonValueKind.Number)
				return false;

			if (!TryGetPropertyIgnoreCase(locationElement, "Y", out JsonElement yElement) || yElement.ValueKind != JsonValueKind.Number)
				return false;

			double dx = xElement.GetDouble() - radius.X;
			double dy = yElement.GetDouble() - radius.Y;
			return (dx * dx) + (dy * dy) <= radius.Radius * radius.Radius;
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

				output[propertyMap.Keys
					.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase))]
						= JsonSerializer.Deserialize<object>(value.GetRawText()) ?? null!; //keep nulls as nulls, not as empty objects or arrays
			}

			return output;
		}

		private McpResponse JsonResponse(object id, object payload)
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

		private object BuildErrorPayload(string code, string message, string? failedSegment)
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

			result = property.GetString()?.Trim();
			return true;
		}

		private static bool TryReadOptionalLocationRadius(JsonElement value, out LocationRadius? locationRadius, out string? error)
		{
			locationRadius = null;
			error = null;
			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("locationRadius", out JsonElement property))
				return true;

			if (property.ValueKind == JsonValueKind.Null)
				return true;

			if (property.ValueKind != JsonValueKind.Object)
			{
				error = "Property 'locationRadius' must be an object with x, y and radius.";
				return false;
			}

			if (!TryReadRequiredInt(property, "x", out int x, out error)) return false;
			if (!TryReadRequiredInt(property, "y", out int y, out error)) return false;
			if (!TryReadRequiredDouble(property, "radius", out double radius, out error)) return false;

			if (x < 0 || y < 0 || radius < 0)
			{
				error = "Property 'locationRadius' requires non-negative x, y and radius.";
				return false;
			}

			locationRadius = new LocationRadius(x, y, radius);
			return true;
		}

		private static bool TryReadRequiredInt(JsonElement value, string propertyName, out int result, out string? error)
		{
			result = 0;
			error = null;
			if (!value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out result))
			{
				error = $"Property 'locationRadius.{propertyName}' must be an integer.";
				return false;
			}

			return true;
		}

		private static bool TryReadRequiredDouble(JsonElement value, string propertyName, out double result, out string? error)
		{
			result = 0;
			error = null;
			if (!value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out result))
			{
				error = $"Property 'locationRadius.{propertyName}' must be a number.";
				return false;
			}

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

		private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement propertyValue)
		{
			foreach (JsonProperty property in element.EnumerateObject())
			{
				if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
				{
					propertyValue = property.Value;
					return true;
				}
			}

			propertyValue = default;
			return false;
		}

		private sealed record LocationRadius(int X, int Y, double Radius);
	}
}