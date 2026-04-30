using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Enums;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;

namespace CivOne.Mcp.Tools
{
	public sealed class GameGetSettingsToolHandler : IMcpToolHandler
	{
		private readonly IRuntime _runtime;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_settings";

		public ToolDefinition Definition => new(
			"game_get_settings",
			"Returns persistent and runtime settings. Optionally filter by dotted keys.",
			new
			{
				type = "object",
				properties = new
				{
					keys = new
					{
						type = "array",
						items = new { type = "string" },
						description = "Optional list of dotted keys to include, for example 'display.graphicsMode' or 'runtime.mcpEnabled'."
					}
				}
			});

		public GameGetSettingsToolHandler(IRuntime runtime, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
		{
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
			_maxJsonChars = maxJsonChars;
		}

		public McpResponse Handle(McpRequest request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));

			if (!ValidateParamsObject(request, out McpResponse validationError))
				return validationError;

			if (!TryReadKeys(request.Params, out string[] keys, out string keysError))
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", keysError, "keys"));

			Dictionary<string, object> flatSettings = new(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, object> groupedSettings = BuildSettingsPayload(flatSettings);

			if (keys.Length == 0)
				return JsonResponse(request.Id, BuildSuccessPayload(groupedSettings));

			HashSet<string> invalidKeys = [];
			Dictionary<string, object> selected = new(StringComparer.OrdinalIgnoreCase);
			foreach (string key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!flatSettings.TryGetValue(key, out object value))
				{
					invalidKeys.Add(key);
					continue;
				}

				selected[key] = value;
			}

			if (invalidKeys.Count > 0)
			{
				return JsonResponse(request.Id, BuildErrorPayload(
					"INVALID_KEYS",
					$"Unknown keys: {string.Join(", ", invalidKeys.OrderBy(x => x))}",
					"keys"));
			}

			return JsonResponse(request.Id, BuildSuccessPayload(selected));
		}

		private Dictionary<string, object> BuildSettingsPayload(IDictionary<string, object> flatSettings)
		{
			Settings settings = Settings.Instance;

			Dictionary<string, object> paths = [];
			AddSetting(paths, flatSettings, "paths", "storageDirectory", settings.StorageDirectory);
			AddSetting(paths, flatSettings, "paths", "captureDirectory", settings.CaptureDirectory);
			AddSetting(paths, flatSettings, "paths", "dataDirectory", settings.DataDirectory);
			AddSetting(paths, flatSettings, "paths", "pluginsDirectory", settings.PluginsDirectory);
			AddSetting(paths, flatSettings, "paths", "savesDirectory", settings.SavesDirectory);
			AddSetting(paths, flatSettings, "paths", "cosSavesDirectory", settings.CosSavesDirectory);
			AddSetting(paths, flatSettings, "paths", "soundsDirectory", settings.SoundsDirectory);

			Dictionary<string, object> display = [];
			AddSetting(display, flatSettings, "display", "windowTitle", settings.WindowTitle);
			AddSetting(display, flatSettings, "display", "graphicsMode", BuildEnumValue(settings.GraphicsMode, settings.GraphicsMode.ToText()));
			AddSetting(display, flatSettings, "display", "fullScreen", BuildBooleanValue(settings.FullScreen));
			AddSetting(display, flatSettings, "display", "scale", settings.Scale);
			AddSetting(display, flatSettings, "display", "aspectRatio", BuildEnumValue(settings.AspectRatio, settings.AspectRatio.ToText()));
			AddSetting(display, flatSettings, "display", "expandWidth", settings.ExpandWidth);
			AddSetting(display, flatSettings, "display", "expandHeight", settings.ExpandHeight);
			// TODO: currently not implemented: because this feature is in another branch!
			AddSetting(display, flatSettings, "display", "windowWidth", "currently not implemented");
			AddSetting(display, flatSettings, "display", "windowHeight", "currently not implemented");
			AddSetting(display, flatSettings, "display", "windowPosX", "currently not implemented");
			AddSetting(display, flatSettings, "display", "windowPosY", "currently not implemented");
			AddSetting(display, flatSettings, "display", "cursorType", BuildEnumValue(settings.CursorType, settings.CursorType.ToText()));
			AddSetting(display, flatSettings, "display", "destroyAnimation", BuildEnumValue(settings.DestroyAnimation, settings.DestroyAnimation.ToText()));

			Dictionary<string, object> patches = [];
			AddSetting(patches, flatSettings, "patches", "rightSideBar", BuildBooleanValue(settings.RightSideBar));
			AddSetting(patches, flatSettings, "patches", "revealWorld", BuildBooleanValue(settings.RevealWorld));
			AddSetting(patches, flatSettings, "patches", "debugMenu", BuildBooleanValue(settings.DebugMenu));
			AddSetting(patches, flatSettings, "patches", "deityEnabled", BuildBooleanValue(settings.DeityEnabled));
			AddSetting(patches, flatSettings, "patches", "arrowHelper", BuildBooleanValue(settings.ArrowHelper));
			AddSetting(patches, flatSettings, "patches", "customMapSize", BuildBooleanValue(settings.CustomMapSize));
			AddSetting(patches, flatSettings, "patches", "pathFinding", BuildBooleanValue(settings.PathFinding));
			AddSetting(patches, flatSettings, "patches", "autoSettlers", BuildBooleanValue(settings.AutoSettlers));
			AddSetting(patches, flatSettings, "patches", "riverFastMovement", BuildBooleanValue(settings.RiverFastMovement));
			AddSetting(patches, flatSettings, "patches", "canalCity", BuildBooleanValue(settings.CanalCity));
			AddSetting(patches, flatSettings, "patches", "preferSveSaveFormat", BuildBooleanValue(settings.PreferSveSaveFormat));
			AddSetting(patches, flatSettings, "patches", "useUncheckedCastSanitizer", BuildBooleanValue(settings.UseUncheckedCastSanitizer));
			AddSetting(patches, flatSettings, "patches", "globalWarmingFeatureFlags", BuildFlagsValue(settings.GlobalWarmingFeatureFlags));

			Dictionary<string, object> gameOptions = [];
			AddSetting(gameOptions, flatSettings, "gameOptions", "instantAdvice", BuildEnumValue(settings.InstantAdvice, settings.InstantAdvice.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "autoSave", BuildEnumValue(settings.AutoSave, settings.AutoSave.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "endOfTurn", BuildEnumValue(settings.EndOfTurn, settings.EndOfTurn.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "animations", BuildEnumValue(settings.Animations, settings.Animations.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "sound", BuildEnumValue(settings.Sound, settings.Sound.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "enemyMoves", BuildEnumValue(settings.EnemyMoves, settings.EnemyMoves.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "civilopediaText", BuildEnumValue(settings.CivilopediaText, settings.CivilopediaText.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "palace", BuildEnumValue(settings.Palace, settings.Palace.ToText()));
			AddSetting(gameOptions, flatSettings, "gameOptions", "taxRate", new { value = settings.TaxRate, percent = settings.TaxRate * 10, text = $"{settings.TaxRate * 10}%" });

			Dictionary<string, object> runtime = [];
			AddSetting(runtime, flatSettings, "runtime", "demo", BuildBooleanValue(_runtime.Settings.Demo, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "setup", BuildBooleanValue(_runtime.Settings.Setup, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "dataCheck", BuildBooleanValue(_runtime.Settings.DataCheck, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "mcpEnabled", BuildBooleanValue(_runtime.Settings.McpEnabled, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "mcpNoAuth", BuildBooleanValue(_runtime.Settings.McpNoAuth, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "free", BuildBooleanValue(_runtime.Settings.Free, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "showCredits", BuildBooleanValue(_runtime.Settings.ShowCredits, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "showIntro", BuildBooleanValue(_runtime.Settings.ShowIntro, "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "loadSaveGameSlot", BuildLoadSaveGameSlotValue(_runtime.Settings.LoadSaveGameSlot));
			AddSetting(runtime, flatSettings, "runtime", "loadCosFile", _runtime.Settings.LoadCosFile);
			AddSetting(runtime, flatSettings, "runtime", "initialSeed", _runtime.Settings.InitialSeed);
			AddSetting(runtime, flatSettings, "runtime", "profileName", _runtime.Settings.Get<string>("profile-name"));
			AddSetting(runtime, flatSettings, "runtime", "noSound", BuildBooleanValue(_runtime.Settings.Get<bool>("no-sound")));
			AddSetting(runtime, flatSettings, "runtime", "softwareRender", BuildBooleanValue(_runtime.Settings.Get<bool>("software-render")));
			AddSetting(runtime, flatSettings, "runtime", "mcpArtifacts", _runtime.Settings.Get<string>("mcp-artifacts"));
			AddSetting(runtime, flatSettings, "runtime", "mcpSaves", _runtime.Settings.Get<string>("mcp-saves"));
			AddSetting(runtime, flatSettings, "runtime", "mcpMaxJsonChars", _runtime.Settings.Get<int>("mcp-max-json-chars"));
			AddSetting(runtime, flatSettings, "runtime", "mcpHttp", BuildBooleanValue(_runtime.Settings.Get<bool>("mcp-http"), "Enabled", "Disabled"));
			AddSetting(runtime, flatSettings, "runtime", "mcpHttpPort", _runtime.Settings.Get<int>("mcp-http-port"));
			AddSetting(runtime, flatSettings, "runtime", "mcpHttpTimeoutMs", _runtime.Settings.Get<int>("mcp-http-timeout-ms"));

			return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["paths"] = paths,
				["display"] = display,
				["patches"] = patches,
				["gameOptions"] = gameOptions,
				["runtime"] = runtime
			};
		}

		private static void AddSetting(IDictionary<string, object> category, IDictionary<string, object> flatSettings, string categoryName, string key, object value)
		{
			category[key] = value;
			flatSettings[$"{categoryName}.{key}"] = value;
		}

		private static object BuildEnumValue<TEnum>(TEnum value, string text) where TEnum : struct, Enum
			=> new
			{
				value = Convert.ToInt32(value),
				name = value.ToString(),
				text = text ?? value.ToString()
			};

		private static object BuildFlagsValue<TEnum>(TEnum value) where TEnum : struct, Enum
		{
			IEnumerable<string> names = Enum.GetValues(typeof(TEnum))
				.Cast<object>()
				.Select(x => (Enum)x)
				.Where(x => Convert.ToInt32(x) != 0 && ((Enum)(object)value).HasFlag(x))
				.Select(x => x.ToString());

			string[] activeNames = [.. names];
			return new
			{
				value = Convert.ToInt32(value),
				names = activeNames,
				text = activeNames.Length > 0 ? string.Join(", ", activeNames) : "None"
			};
		}

		private static object BuildBooleanValue(bool value, string trueText = "On", string falseText = "Off")
			=> new { value, text = value ? trueText : falseText };

		private static object BuildLoadSaveGameSlotValue(Tuple<char, int> loadSaveGameSlot)
		{
			if (loadSaveGameSlot == null)
				return null;

			bool usesLoadingScreen = loadSaveGameSlot.Item2 < 0;
			return new
			{
				drive = loadSaveGameSlot.Item1.ToString(),
				slot = loadSaveGameSlot.Item2,
				usesLoadingScreen,
				text = usesLoadingScreen
					? "Loading screen"
					: $"{loadSaveGameSlot.Item1}{loadSaveGameSlot.Item2}"
			};
		}

		private McpResponse JsonResponse(object id, object payload)
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

		private static bool TryReadKeys(JsonElement value, out string[] keys, out string error)
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

				string text = item.GetString();
				if (!string.IsNullOrWhiteSpace(text))
					parsed.Add(text.Trim());
			}

			keys = [.. parsed];
			return true;
		}
	}
}