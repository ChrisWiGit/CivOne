using System;
using System.Collections.Generic;
using System.Text.Json;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	/// <summary>
	/// MCP tool handler for <c>game_get_entities_index</c>.
	/// Returns a compact cross-reference of all players and cities with their
	/// stable GUIDs and display names – a fast, low-payload starting point for
	/// any LLM-driven automation that needs to refer to entities across turns.
	/// </summary>
	public sealed class GameGetEntitiesIndexToolHandler : IMcpToolHandler
	{
		private readonly IGameStateDtoSnapshotProvider _snapshotProvider;
		private readonly JsonSaveGameStateWriter _jsonWriter;
		private readonly int _maxJsonChars;

		public string Method => "game_get_entities_index";

		public ToolDefinition Definition => new(
			"game_get_entities_index",
			"Returns a compact index of all players and cities with their stable GUIDs and display names. Use this as a cheap first call to discover entity identifiers before querying details.",
			new
			{
				type = "object",
				properties = new { }
			});

		public GameGetEntitiesIndexToolHandler(
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
			if (request == null) throw new ArgumentNullException(nameof(request));

			if (request.Params.ValueKind != JsonValueKind.Undefined &&
				request.Params.ValueKind != JsonValueKind.Null &&
				request.Params.ValueKind != JsonValueKind.Object)
			{
				return JsonResponse(request.Id, BuildErrorPayload("INVALID_PARAMS", "'params' must be an object."));
			}

			if (!_snapshotProvider.TryGetSnapshot(out GameStateDto snapshot, out string errorCode, out string errorMessage))
				return JsonResponse(request.Id, BuildErrorPayload(errorCode, errorMessage));

			List<PlayerDto> players = snapshot.Players ?? [];
			List<object> playerEntries = [];
			List<object> cityEntries = [];

			for (int i = 0; i < players.Count; i++)
			{
				PlayerDto player = players[i];
				playerEntries.Add(new
				{
					id = player.Id,
					playerGuid = player.PlayerGuid,
					tribeName = player.TribeName
				});

				foreach (CityDto city in player.Cities ?? [])
				{
					cityEntries.Add(new
					{
						id = city.Id,
						name = city.Name,
						owner = player.Id,
						ownerGuid = player.PlayerGuid
					});
				}
			}

			return JsonResponse(request.Id, BuildSuccessPayload(new
			{
				gameTurn = snapshot.GameTurn,
				players = playerEntries,
				cities = cityEntries
			}));
		}

		private McpResponse JsonResponse(object id, object payload)
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

		private object BuildErrorPayload(string code, string message)
		{
			return new
			{
				ok = false,
				truncated = false,
				maxChars = _maxJsonChars,
				returnedChars = 0,
				error = new { code, message }
			};
		}
	}
}
