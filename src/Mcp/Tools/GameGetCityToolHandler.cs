using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Enums;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetCityToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_city";

		public ToolDefinition Definition => new(
			"game_get_city",
			"Returns one city by cityId, cityName, or cityNameStartsWith including compact production/unrest/happiness views.",
			new
			{
				type = "object",
				properties = new
				{
					cityId = new { type = "string", description = "Optional city GUID selector." },
					cityName = new { type = "string", description = "Optional exact city name selector (case-insensitive)." },
					cityNameStartsWith = new { type = "string", description = "Optional prefix city name selector (case-insensitive)." },
					keys = new
					{
						type = "array",
						items = new { type = "string" },
						description = "Optional list of keys to include. If omitted, all keys are returned."
					}
				}
			});

		public GameGetCityToolHandler(
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

			if (!TryReadOptionalGuid(request.Params, "cityId", out Guid? cityId, out string? cityIdError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", cityIdError!, "cityId"));
	
			if (!TryReadOptionalString(request.Params, "cityName", out string? cityName, out string? cityNameError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", cityNameError!, "cityName"));

			if (!TryReadOptionalString(request.Params, "cityNameStartsWith", out string? cityNameStartsWith, out string? cityNameStartsWithError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", cityNameStartsWithError!, "cityNameStartsWith"));

			if (!TryReadKeys(request.Params, out string[] keys, out string? keysError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", keysError!, "keys"));

			int selectorCount = CountSelectors(cityId, cityName, cityNameStartsWith!);
			if (selectorCount == 0)
				return JsonResponse(request.Id, BuildErrorPayload("MISSING_SELECTOR", "Provide one selector: cityId, cityName, or cityNameStartsWith.", "cityId|cityName|cityNameStartsWith"));

			if (selectorCount > 1)
				return JsonResponse(request.Id, BuildErrorPayload("AMBIGUOUS_SELECTOR", "Provide only one selector: cityId, cityName, or cityNameStartsWith.", "cityId|cityName|cityNameStartsWith"));

			List<(CityDto City, PlayerDto Owner)> allCities = GetAllCities(snapshot);
			List<(CityDto City, PlayerDto Owner)> matches = FindMatches(allCities, cityId, cityName, cityNameStartsWith!);

			if (matches.Count == 0)
				return JsonResponse(request.Id, BuildErrorPayload("CITY_NOT_FOUND", "No city matched the provided selector.", "cityId|cityName|cityNameStartsWith"));

			if (matches.Count > 1)
			{
				string names = string.Join(", ", matches.Select(x => x.City.Name).Distinct(StringComparer.OrdinalIgnoreCase).Take(5));
				return JsonResponse(request.Id, BuildErrorPayload("AMBIGUOUS_CITY", $"Selector matched multiple cities ({matches.Count}). Examples: {names}", "cityName|cityNameStartsWith"));
			}

			(CityDto city, PlayerDto owner) = matches[0];
			object enriched = BuildEnrichedCity(city, owner);

			if (keys.Length == 0)
				return JsonResponse(request.Id, BuildSuccessPayload(enriched));

			HashSet<string> invalidKeys = new(StringComparer.OrdinalIgnoreCase);
			object projected = ProjectByKeys(enriched, keys, invalidKeys);

			if (invalidKeys.Count > 0)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_KEYS", $"Unknown keys: {string.Join(", ", invalidKeys.OrderBy(x => x))}", "keys"));

			return JsonResponse(request.Id, BuildSuccessPayload(projected));
		}

		private static int CountSelectors(Guid? cityId, string? cityName, string? cityNameStartsWith)
			=> (cityId.HasValue ? 1 : 0)
			+ (!string.IsNullOrWhiteSpace(cityName) ? 1 : 0)
			+ (!string.IsNullOrWhiteSpace(cityNameStartsWith) ? 1 : 0);

		private static List<(CityDto City, PlayerDto Owner)> GetAllCities(GameStateDto snapshot)
		{
			List<(CityDto City, PlayerDto Owner)> cities = [];
			foreach (PlayerDto player in snapshot.Players ?? [])
			{
				foreach (CityDto city in player.Cities ?? [])
					cities.Add((city, player));
			}

			return cities;
		}

		private static List<(CityDto City, PlayerDto Owner)> FindMatches(
			IEnumerable<(CityDto City, PlayerDto Owner)> cities,
			Guid? cityId,
			string? cityName,
			string cityNameStartsWith)
		{
			if (cityId.HasValue)
				return [.. cities.Where(x => x.City.Id == cityId.Value)];

			if (!string.IsNullOrWhiteSpace(cityName))
				return [.. cities.Where(x => string.Equals(x.City.Name, cityName, StringComparison.OrdinalIgnoreCase))];

			return [.. cities.Where(x => !string.IsNullOrWhiteSpace(x.City.Name) && x.City.Name.StartsWith(cityNameStartsWith, StringComparison.OrdinalIgnoreCase))];
		}

		private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		private Dictionary<string, object> BuildEnrichedCity(CityDto city, PlayerDto owner)
		{
			Dictionary<string, object> baseData = 
				JsonSerializer.Deserialize<Dictionary<string, object>>(_jsonWriter.AsString(city), _jsonOptions) ?? 
					new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			baseData["ownerGuid"] = owner?.PlayerGuid ?? Guid.Empty;
			baseData["ownerTribeName"] = owner?.TribeName!;
			baseData["productionView"] = BuildProductionView(city);
			baseData["unrestView"] = BuildUnrestView(city);
			baseData["happinessView"] = BuildHappinessView(city);

			return baseData;
		}

		private static object BuildProductionView(CityDto city)
		{
			if (city.CurrentProduction == null)
			{
				return new
				{
					productionId = (uint?)null,
					price = (uint?)null,
					buyPrice = (uint?)null,
					storedShields = city.Shields,
					remainingShields = (int?)null
				};
			}

			int remainingShields = Math.Max(0, (int)city.CurrentProduction.Price - city.Shields);
			return new
			{
				productionId = city.CurrentProduction.ProductionId,
				price = city.CurrentProduction.Price,
				buyPrice = city.CurrentProduction.BuyPrice,
				storedShields = city.Shields,
				remainingShields
			};
		}

		private static object BuildUnrestView(CityDto city)
		{
			bool isRiot = HasStatus(city, CityStatusEnum.Riot);
			bool isCelebrating = HasStatus(city, CityStatusEnum.CelebrationRapture);
			bool celebrationCancelled = HasStatus(city, CityStatusEnum.CelebrationCancelled);
			return new
			{
				isRiot,
				wasInDisorder = city.WasInDisorder,
				isCelebrating,
				celebrationCancelled
			};
		}

		private static object BuildHappinessView(CityDto city)
		{
			int entertainerCount = (city.Specialists ?? []).Count(x => x == Citizen.Entertainer);
			int specialistCount = city.Specialists?.Count ?? 0;

			string mood = "Stable";
			if (HasStatus(city, CityStatusEnum.Riot))
				mood = "Riot";
			else if (HasStatus(city, CityStatusEnum.CelebrationRapture) && !HasStatus(city, CityStatusEnum.CelebrationCancelled))
				mood = "Celebration";
			else if (city.WasInDisorder)
				mood = "Recovering";

			return new
			{
				size = city.Size,
				entertainerCount,
				specialistCount,
				mood
			};
		}

		private static bool HasStatus(CityDto city, CityStatusEnum status)
			=> (city.Status ?? []).Contains(status);

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