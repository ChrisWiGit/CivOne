using System;
using System.IO;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Factories;

namespace CivOne.Mcp.Tools
{
	public sealed class GameSaveToolHandler : IMcpToolHandler
	{
		private readonly IRuntime _runtime;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;
		private readonly Func<DateTimeOffset> _utcNowProvider;

		public string Method => "game_save";

		public ToolDefinition Definition => new(
			"game_save",
			"Saves the current game state as a new .cos file in the configured MCP saves folder using savegame_mcp_<UTC yyyyMMddHHmmssfff>.cos. Existing files are never overwritten.",
			new
			{
				type = "object",
				properties = new { }
			});

		public GameSaveToolHandler(IRuntime runtime, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
			: this(runtime, jsonWriter, maxJsonChars, () => DateTimeOffset.UtcNow)
		{
		}

		internal GameSaveToolHandler(IRuntime runtime, JsonSaveGameStateWriter jsonWriter, int maxJsonChars, Func<DateTimeOffset> utcNowProvider)
		{
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_utcNowProvider = utcNowProvider ?? throw new ArgumentNullException(nameof(utcNowProvider));
			_maxJsonChars = Math.Max(512, maxJsonChars);
		}

		public McpResponse Handle(McpRequest request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			if (!ValidateParamsObject(request, out McpResponse validationError))
				return validationError;

			if (!Game.Started || Game.Instance == null)
				return JsonResponse(request.Id, BuildErrorPayload("GAME_NOT_STARTED", "No active game to save.", null));

			string saveFolder = ResolveSaveFolder();
			if (!Directory.Exists(saveFolder))
				return JsonResponse(request.Id, BuildErrorPayload("SAVE_FOLDER_NOT_FOUND", "Configured MCP saves folder does not exist.", "saveFolder"));

			string timestamp = _utcNowProvider().ToUniversalTime().ToString("yyyyMMddHHmmssfff");
			string fileName = $"savegame_mcp_{timestamp}.cos";
			string fullPath = Path.Combine(saveFolder, fileName);

			if (File.Exists(fullPath))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"FILE_EXISTS",
					"file exists, wait a second, till next try",
					"fileName"));
			}

			var game = Game.Instance;
			if (game == null)
				return JsonResponse(request.Id, BuildErrorPayload("GAME_NOT_STARTED", "No active game to save.", null));

			Guid saveGuid = Guid.NewGuid();
			game.SaveMetaData.RestoreSaveGuid(saveGuid);
			game.SaveMetaData.DisplayName = game.SaveMetaDataService.BuildDisplayName(game.Difficulty, game.HumanPlayer, game.GameTurn);

			try
			{
				GameStateHandler gameState = new();
				var mapperDependencies = YamlMapperDependenciesFactory
					.CreateDefault()
					.Create(game);
				YamlSaveGameStateWriter writer = new(
					mapperDependencies.PlayerMapper,
					mapperDependencies.UnitMapper,
					mapperDependencies.MapMapper,
					mapperDependencies.GlobalWarmingMapper,
					mapperDependencies.Sanitizer);

				using (FileStream stream = new(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					writer.Write(stream, gameState.Create(game), game.SaveMetaData);
					stream.Flush();
				}

				game.MarkAsYamlSaveSource();
			}
			catch (IOException ex) when (File.Exists(fullPath))
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"FILE_EXISTS",
					$"file exists, wait a second, till next try ({ex.Message})",
					"fileName"));
			}
			catch (Exception ex)
			{
				return JsonResponse(request.Id, BuildErrorPayload("SAVE_FAILED", ex.Message, "fileName"));
			}

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				fileName,
				saveGuid
			}));
		}

		private string ResolveSaveFolder()
		{
			string configuredFolder = _runtime.Settings.Get<string>("mcp-saves");
			if (string.IsNullOrWhiteSpace(configuredFolder))
				configuredFolder = Settings.Instance.CosSavesDirectory;

			return Path.GetFullPath(configuredFolder);
		}

		private McpResponse JsonResponse(object id, object payload)
			=> McpJsonToolResponse.JsonResponse(id, payload, _jsonWriter, _maxJsonChars);

		private object BuildSuccessPayload(object data)
		{
			string payloadJson = _jsonWriter.AsString(data ?? new { });
			return new
			{
				ok = true,
				path = (string)null,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = payloadJson.Length,
				data = data ?? new { }
			};
		}

		private object BuildErrorPayload(string code, string message, string failedSegment)
		{
			return new
			{
				ok = false,
				path = (string)null,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new
				{
					code,
					message,
					path = (string)null,
					failedSegment
				}
			};
		}

		private static bool ValidateParamsObject(McpRequest request, out McpResponse response)
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
