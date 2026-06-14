using System;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetStateToolHandler : IMcpToolHandler
	{
		public const int MaxJsonCharsDefault = 32000;

		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly McpGameStatePathResolver _pathResolver;
		private readonly int _maxJsonChars;

		public string Method => "game_get_state";

		public ToolDefinition Definition => new(
			"game_get_state",
			"Returns a selected part of the current game state using a dot path. Output is always JSON.",
			new
			{
				type = "object",
				properties = new
				{
					path = new
					{
						type = "string",
						description = "Dot-separated path into the game state (e.g. 'GameState.Players[0].Civilization')."
					}
				}
			});

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (request.Params.ValueKind != JsonValueKind.Undefined &&
				request.Params.ValueKind != JsonValueKind.Null &&
				request.Params.ValueKind != JsonValueKind.Object)
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_PARAMS",
					"'params' must be an object.",
					null,
					null));
			}

			if (request.Params.ValueKind == JsonValueKind.Object &&
				request.Params.TryGetProperty("path", out JsonElement pathElement) &&
				pathElement.ValueKind != JsonValueKind.String &&
				pathElement.ValueKind != JsonValueKind.Null)
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_PARAMS",
					"Property 'path' must be a string.",
					null,
					"path"));
			}

			string? path = ReadString(request.Params, "path");
			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null, null));

			if (string.IsNullOrWhiteSpace(path))
				return JsonResponse(request.Id, BuildSuccessPayload(path, BuildSummary(snapshot!)));

			if (string.Equals(path, "GameState", StringComparison.Ordinal))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"PATH_TOO_BROAD",
					"Path 'GameState' is too broad. Request a specific subset, e.g. 'GameState.Players[0]' or 'GameState.Map'.",
					path,
					"GameState"));
			}

			string normalizedPath = path.Trim();
			string rootPath = normalizedPath.Split('.')[0];
			if (!string.Equals(rootPath, "GameState", StringComparison.Ordinal))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_PATH_ROOT",
					"Path must start with 'GameState'.",
					normalizedPath,
					rootPath));
			}

			string rootJson = _jsonWriter.AsString(new { GameState = snapshot });
			using JsonDocument jsonDocument = JsonDocument.Parse(rootJson);
			JsonElement root = jsonDocument.RootElement;

			if (!McpGameStatePathResolver.TryResolve(root, normalizedPath, out JsonElement resolved, out string? failedSegment, out string? pathErrorMessage))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_PATH",
					pathErrorMessage,
					normalizedPath,
					failedSegment));
			}

			object? resolvedObject = JsonSerializer.Deserialize<object>(resolved.GetRawText());
			return JsonResponse(request.Id, BuildSuccessPayload(normalizedPath, resolvedObject));
		}

		private McpResponse JsonResponse(object? id, object payload)
		{
			string json = _jsonWriter.AsString(payload);
			if (json.Length > _maxJsonChars)
			{
				json = BuildTruncatedPayload(json);
			}

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
					error = new
					{
						code = "PAYLOAD_TRUNCATED",
						message = "The payload exceeded the configured size limit and was truncated."
					}
				});

				if (candidate.Length <= _maxJsonChars)
					return candidate;

				int step = Math.Min(256, preview.Length);
				preview = preview[..^step];
			}

			return _jsonWriter.AsString(new
			{
				ok = false,
				truncated = true,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				totalChars = sourceJson.Length,
				error = new
				{
					code = "PAYLOAD_TRUNCATED",
					message = "The payload exceeded the configured size limit and was truncated."
				}
			});
		}

		private static object BuildSummary(GameStateDto snapshot)
		{
			var players = snapshot.Players ?? [];
			var playerSummary = players.Select(player => new
			{
				id = player.Id,
				playerGuid = player.PlayerGuid,
				tribeName = player.TribeName,
				cityCount = player.Cities?.Count ?? 0,
				unitCount = player.Units?.Count ?? 0,
				gold = player.Gold,
				science = player.Science
			});

			return new
			{
				gameTurn = snapshot.GameTurn,
				currentPlayer = snapshot.CurrentPlayer,
				humanPlayer = snapshot.HumanPlayer,
				difficulty = snapshot.Difficulty.ToString(),
				playerCount = players.Count,
				totalCities = players.Sum(x => x.Cities?.Count ?? 0),
				totalUnits = players.Sum(x => x.Units?.Count ?? 0),
				players = playerSummary
			};
		}

		private object BuildSuccessPayload(string? path, object? data)
		{
			string payloadJson = _jsonWriter.AsString(data ?? new { });
			return new
			{
				ok = true,
				path,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = payloadJson.Length,
				data = data ?? new { }
			};
		}

		private object BuildErrorPayload(string code, string? message, string? path, string? failedSegment)
		{
			return new
			{
				ok = false,
				path,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new
				{
					code,
					message,
					path,
					failedSegment
				}
			};
		}

		private static string? ReadString(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property)
				? property.GetString()
				: null;

		public GameGetStateToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars = MaxJsonCharsDefault)
		{
			_snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_pathResolver = new McpGameStatePathResolver();
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}
	}
}
