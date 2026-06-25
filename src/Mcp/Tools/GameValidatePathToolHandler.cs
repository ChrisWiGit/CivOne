using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	/// <summary>
	/// MCP tool handler for <c>game_validate_path</c>.
	/// Resolves a dot-notation path against the current game state and reports
	/// whether the path is valid, without returning the full node payload.
	/// Useful for LLM agents that want to check a path before calling
	/// <c>game_get_state</c> with it.
	/// </summary>
	public sealed class GameValidatePathToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly McpGameStatePathResolver _pathResolver;
		private readonly int _maxJsonChars;

		public string Method => "game_validate_path";

		public ToolDefinition Definition => new(
			"game_validate_path",
			"Validates a dot-notation GameState path without returning the full payload. Returns whether the path resolves, the value kind at the resolved node, and error details if it fails.",
			new
			{
				type = "object",
				required = InputSchema,
				properties = new
				{
					path = new
					{
						type = "string",
						description = "Dot-notation path into the game state (e.g. 'GameState.Players[0].Gold')."
					}
				}
			});

		private static readonly string[] InputSchema = new[] { "path" };

		public GameValidatePathToolHandler(
			IGameStateDtoSnapshotProvider snapshotProvider,
			JsonSaveGameStateWriter jsonWriter,
			int maxJsonChars = GameGetStateToolHandler.MaxJsonCharsDefault)
		{
			_snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_pathResolver = new McpGameStatePathResolver();
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}

		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "valueKind: We want to return the value kind in lowercase for consistency.")]
		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (request.Params.ValueKind != JsonValueKind.Undefined &&
				request.Params.ValueKind != JsonValueKind.Null &&
				request.Params.ValueKind != JsonValueKind.Object)
			{
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", "'params' must be an object.", null, null));
			}

			string? path = ReadString(request.Params, "path");
			if (string.IsNullOrWhiteSpace(path))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"MISSING_PARAM",
					"Property 'path' is required.",
					null,
					"path"));
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

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode, errorMessage, null, null));

			string normalizedPath = path.Trim();
			string rootSegment = normalizedPath.Split('.')[0];
			if (!string.Equals(rootSegment, "GameState", StringComparison.Ordinal))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_PATH_ROOT",
					"Path must start with 'GameState'.",
					normalizedPath,
					rootSegment));
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

			string valueKind = resolved.ValueKind.ToString().ToLowerInvariant();

			return JsonResponse(request.Id, new
			{
				ok = true,
				valid = true,
				path = normalizedPath,
				valueKind,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0
			});
		}

		private McpResponse JsonResponse(object? id, object payload)
			=> McpJsonToolResponse.JsonResponse(id, payload, _jsonWriter, _maxJsonChars);

		private object BuildErrorPayload(string code, string? message, string? path, string? failedSegment)
		{
			return new
			{
				ok = false,
				valid = false,
				path,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new { code, message, path, failedSegment }
			};
		}

		private static string? ReadString(JsonElement value, string propertyName)
			=> value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out JsonElement property)
				? property.GetString()
				: null;
	}
}
