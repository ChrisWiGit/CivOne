using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Screens;

namespace CivOne.Mcp.Tools
{
	public sealed class GameLoadToolHandler : IMcpToolHandler
	{
		private readonly IRuntime _runtime;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_load";

		public ToolDefinition Definition => new(
			"game_load",
			"Loads a .cos save by file name or saveGuid from the configured MCP saves folder. Provide exactly one of fileName or saveGuid.",
			new
			{
				type = "object",
				properties = new
				{
					fileName = new
					{
						type = "string",
						description = "Save file name (for example: savegame_11.cos). File name only, no path segments."
					},
					saveGuid = new
					{
						type = "string",
						format = "uuid",
						description = "Stable save GUID as returned by game_list_saves. Scans all .cos files in the saves folder."
					}
				}
			});

		public GameLoadToolHandler(IRuntime runtime, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
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

			TryReadOptionalFileName(request.Params, out string? fileName);
			TryReadOptionalSaveGuid(request.Params, out Guid saveGuid);

			if (string.IsNullOrWhiteSpace(fileName) && saveGuid == Guid.Empty)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", "Provide exactly one of 'fileName' or 'saveGuid'.", null));

			if (!string.IsNullOrWhiteSpace(fileName) && saveGuid != Guid.Empty)
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", "Provide exactly one of 'fileName' or 'saveGuid', not both.", null));

			string saveFolder = ResolveSaveFolder();
			if (!Directory.Exists(saveFolder))
				return JsonResponse(request.Id, BuildErrorPayload("SAVE_FOLDER_NOT_FOUND", "Configured MCP saves folder does not exist.", "saveFolder"));

			string? fullPath = null;
			if (saveGuid != Guid.Empty)
			{
				if (!TryFindByGuid(saveFolder, saveGuid, out fullPath, out string? resolvedFileName))
					return JsonResponse(request.Id, BuildErrorPayload("FILE_NOT_FOUND", $"No .cos file with saveGuid '{saveGuid}' found in saves folder.", "saveGuid"));
				fileName = resolvedFileName!;
			}
			else
			{
				if (!TryResolveSafeCosPath(saveFolder, fileName!, out fullPath, out string? pathError))
					return JsonResponse(request.Id, BuildErrorPayload("INVALID_FILE_NAME", pathError, "fileName"));

				if (!File.Exists(fullPath))
					return JsonResponse(request.Id, BuildErrorPayload("FILE_NOT_FOUND", "Save file not found.", "fileName"));
			}

			if (!CosSaveFileInspector.TryInspect(fullPath, out CosSaveFileInspection? inspection))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_SAVE_FILE", "Save file could not be parsed as a valid .cos save.", "fileName"));

			bool loaded = Game.LoadYamlGame(fullPath!);
			if (!loaded)
				return JsonResponse(request.Id, BuildErrorPayload("LOAD_FAILED", "Game.LoadYamlGame returned false.", "fileName"));

			Common.DestroyScreen(Common.Screens.FirstOrDefault(s => s is GamePlay));
			Common.AddScreen(new GamePlay());

			Guid effectiveSaveGuid = Game.Started ? Game.Instance.SaveMetaData.SaveGuid : inspection!.SaveGuid ?? Guid.Empty;
			if (effectiveSaveGuid == Guid.Empty)
				effectiveSaveGuid = Guid.NewGuid();

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				fileName,
				saveGuid = effectiveSaveGuid,
				displayName = string.IsNullOrWhiteSpace(inspection!.Meta?.DisplayName)
					? Path.GetFileNameWithoutExtension(fileName)
					: inspection.Meta.DisplayName,
				gameTurn = inspection.GameState?.GameTurn,
				difficulty = inspection.GameState is null
					? null
					: new
					{
						value = (int)inspection.GameState.Difficulty,
						name = inspection.GameState.Difficulty.ToString()
					}
			}));
		}

		private string ResolveSaveFolder()
		{
			string? configuredFolder = _runtime.Settings.Get<string>("mcp-saves");
			if (string.IsNullOrWhiteSpace(configuredFolder))
				configuredFolder = Settings.Instance.CosSavesDirectory;

			return Path.GetFullPath(configuredFolder);
		}

		private static bool TryFindByGuid(string saveFolder, Guid saveGuid, out string? fullPath, out string? fileName)
		{
			fullPath = null;
			fileName = null;

			foreach (string candidate in Directory.EnumerateFiles(saveFolder, "*.cos", SearchOption.TopDirectoryOnly))
			{
				if (!CosSaveFileInspector.TryInspect(candidate, out CosSaveFileInspection? inspection))
					continue;
				if (inspection!.SaveGuid.HasValue && inspection.SaveGuid.Value == saveGuid)
				{
					fullPath = candidate;
					fileName = Path.GetFileName(candidate);
					return true;
				}
			}
			return false;
		}

		private static bool TryResolveSafeCosPath(string saveFolder, string fileName, out string? fullPath, [NotNullWhen(false)] out string? error)
		{
			fullPath = null;
			error = null;

			if (string.IsNullOrWhiteSpace(fileName))
			{
				error = "Property 'fileName' is required.";
				return false;
			}

			string trimmed = fileName.Trim();
			if (ContainsPathSeparators(trimmed))
			{
				error = "Property 'fileName' must not contain path separators.";
				return false;
			}

			if (!trimmed.EndsWith(".cos", StringComparison.OrdinalIgnoreCase))
			{
				error = "Only .cos files are supported.";
				return false;
			}

			string root = Path.GetFullPath(saveFolder);
			if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				root += Path.DirectorySeparatorChar;

			string candidate = Path.GetFullPath(Path.Combine(root, trimmed));
			if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				error = "Resolved path is outside configured MCP saves folder.";
				return false;
			}

			fullPath = candidate;
			return true;
		}

		private static bool ContainsPathSeparators(string fileName)
		{
			return fileName.Contains('/') || fileName.Contains('\\');
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

		private object BuildErrorPayload(string code, string message, string? failedSegment)
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

		private static void TryReadOptionalFileName(JsonElement value, out string? fileName)
		{
			fileName = null;
			if (value.ValueKind != JsonValueKind.Object) return;
			if (!value.TryGetProperty("fileName", out JsonElement prop)) return;
			if (prop.ValueKind == JsonValueKind.String)
				fileName = prop.GetString()?.Trim();
		}

		private static void TryReadOptionalSaveGuid(JsonElement value, out Guid saveGuid)
		{
			saveGuid = Guid.Empty;
			if (value.ValueKind != JsonValueKind.Object) return;
			if (!value.TryGetProperty("saveGuid", out JsonElement prop)) return;
			if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out Guid parsed))
				saveGuid = parsed;
		}

		[Obsolete("Kept for reference only")]
		private static bool TryReadRequiredFileName(JsonElement value, out string? fileName, [NotNullWhen(false)] out string? error)
		{
			fileName = null;
			error = null;

			if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("fileName", out JsonElement property))
			{
				error = "Property 'fileName' is required.";
				return false;
			}

			if (property.ValueKind != JsonValueKind.String)
			{
				error = "Property 'fileName' must be a string.";
				return false;
			}

			fileName = property.GetString()?.Trim();
			if (string.IsNullOrWhiteSpace(fileName))
			{
				error = "Property 'fileName' must be a non-empty string.";
				return false;
			}

			return true;
		}
	}
}
