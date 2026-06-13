using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;

namespace CivOne.Mcp.Tools
{
	public sealed class GameListSavesToolHandler : IMcpToolHandler
	{
		private readonly IRuntime _runtime;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_list_saves";

		public ToolDefinition Definition => new(
			"game_list_saves",
			"Returns metadata for valid .cos save files in the configured MCP saves folder. Invalid files are omitted.",
			new
			{
				type = "object",
				properties = new { }
			});

		public GameListSavesToolHandler(IRuntime runtime, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
		{
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (!ValidateParamsObject(request, out McpResponse? validationError))
				return validationError!;

			string saveFolder = ResolveSaveFolder();
			if (!Directory.Exists(saveFolder))
				return JsonResponse(request.Id, BuildSuccessPayload(new { saveFolder, saves = Array.Empty<object>() }));

			List<object> saves = [];
			IEnumerable<string> files = Directory
				.EnumerateFiles(saveFolder, "*.cos", SearchOption.TopDirectoryOnly)
				.OrderByDescending(File.GetLastWriteTimeUtc)
				.ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

			foreach (string file in files)
			{
				if (!CosSaveFileInspector.TryInspect(file, out CosSaveFileInspection? inspection))
					continue;

				var gameState = inspection!.GameState;
				var mapTiles = gameState?.Map?.Tiles;
				FileInfo fileInfo = new(file);

				saves.Add(new
				{
					fileName = Path.GetFileName(file),
					formatVersion = inspection.FormatVersion,
					saveGuid = inspection.SaveGuid,
					displayName = string.IsNullOrWhiteSpace(inspection.Meta?.DisplayName)
						? Path.GetFileNameWithoutExtension(file)
						: inspection.Meta.DisplayName,
					gameTurn = gameState?.GameTurn,
					difficulty = gameState is null
						? null
						: new
						{
							value = (int)gameState.Difficulty,
							name = gameState.Difficulty.ToString()
						},
					humanPlayer = gameState?.HumanPlayer,
					currentPlayer = gameState?.CurrentPlayer,
					playerCount = gameState?.Players?.Count ?? 0,
					mapWidth = mapTiles?.Width(),
					mapHeight = mapTiles?.Height(),
					gameStartedAt = inspection.Meta?.GameStartedAt,
					gameVersion = inspection.Meta?.GameVersion,
					playDurationMinutes = inspection.Meta?.PlayDurationMinutes,
					fileSizeBytes = fileInfo.Length,
					lastWriteAtUtc = fileInfo.LastWriteTimeUtc.ToString("O")
				});
			}

			return JsonResponse(request.Id, BuildSuccessPayload(new { saveFolder, saves }));
		}

		private string ResolveSaveFolder()
		{
			string? configuredFolder = _runtime.Settings.Get<string>("mcp-saves");
			if (string.IsNullOrWhiteSpace(configuredFolder))
				configuredFolder = Settings.Instance.CosSavesDirectory;

			return Path.GetFullPath(configuredFolder);
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
	}
}
