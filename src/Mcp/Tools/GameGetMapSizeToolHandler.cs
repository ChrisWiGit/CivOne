using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetMapSizeToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_map_size";

		public ToolDefinition Definition => new(
			"game_get_map_size",
			"Returns map dimensions and wrap semantics for the active game.",
			new
			{
				type = "object",
				properties = new { }
			});

		public GameGetMapSizeToolHandler(
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

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto? snapshot, out string? errorCode, out string? errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode!, errorMessage, null));

			Map2d<TileDto>? tiles = snapshot?.Map?.Tiles;
			if (tiles == null)
				return JsonResponse(request.Id, BuildErrorPayload("NO_MAP", "No map data available.", null));

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				width = tiles.Width(),
				height = tiles.Height(),
				wrapX = true,
				wrapY = false
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
	}
}