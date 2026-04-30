using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Mcp.Automation;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Mcp.Transport;
using CivOne.Persistence;
using CivOne.Persistence.Factories;

namespace CivOne.Mcp
{
	public static class McpServiceFactory
	{
		public static IMcpService Create(IRuntime runtime)
		{
			if (runtime == null) throw new ArgumentNullException(nameof(runtime));
			if (!runtime.Settings.McpEnabled)
				return new McpNoopService();

			string artifactRootFolder = runtime.Settings.Get<string>("mcp-artifacts");
			if (string.IsNullOrWhiteSpace(artifactRootFolder))
				artifactRootFolder = Path.Combine(runtime.StorageDirectory, "temp", "mcp-runs");

			IMcpArtifactWriter artifactWriter = new FileSystemMcpArtifactWriter(artifactRootFolder);
			IMcpGameTickProvider gameTickProvider = new RuntimeHandlerGameTickProvider();
			IMcpScreenshotRoutine screenshotRoutine = new RuntimeLayerScreenshotRoutine(runtime, artifactWriter, gameTickProvider);
			IYamlMapperDependenciesFactory mapperDependenciesFactory = YamlMapperDependenciesFactory.CreateDefault();
			JsonSaveGameStateWriter jsonSaveGameStateWriter = new();
			GameStateHandler gameStateHandler = new();
			IGameStateDtoSnapshotProvider snapshotProvider = new GameStateDtoSnapshotProvider(gameTickProvider, gameStateHandler, mapperDependenciesFactory);
			int maxJsonChars = runtime.Settings.Get<int>("mcp-max-json-chars");
			if (maxJsonChars <= 0)
				maxJsonChars = GameGetStateToolHandler.MaxJsonCharsDefault;

			// Real game tools — these are exposed via tools/list and tools/call
			IMcpToolHandler[] realHandlers =
			[
				new CaptureScreenshotToolHandler(screenshotRoutine),
				new CaptureRegionToolHandler(screenshotRoutine),
				new GameGetSettingsToolHandler(runtime, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetMapSizeToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetMapWindowToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetMapLandValuesWindowToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetVisibilityToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetStateToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetEntitiesIndexToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameValidatePathToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetUnitsToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetCityToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetPlayerToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameListSavesToolHandler(runtime, jsonSaveGameStateWriter, maxJsonChars),
				new GameSaveToolHandler(runtime, jsonSaveGameStateWriter, maxJsonChars),
				new GameLoadToolHandler(runtime, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetPlayersToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars),
				new GameGetCitiesToolHandler(snapshotProvider, jsonSaveGameStateWriter, maxJsonChars)
			];

			IReadOnlyList<ToolDefinition> definitions = realHandlers
				.Select(h => h.Definition)
				.Where(d => d != null)
				.ToList();

			// Standard MCP meta-handlers
			IMcpToolHandler[] metaHandlers =
			[
				new ToolsListHandler(definitions),
				new ToolsCallHandler(realHandlers, msg => runtime.Log("[MCP] {0}", msg))
			];

			IMcpToolRegistry registry = new McpToolRegistry([.. realHandlers, .. metaHandlers]);
			IMcpProtocolSerializer serializer = new JsonRpcProtocolSerializer();
			IMcpTransport transport = new StdioMcpTransport(serializer, message => runtime.Log("[MCP] {0}", message));

			string sessionToken = Guid.NewGuid().ToString("N");
			bool authEnabled = !runtime.Settings.McpNoAuth;
			return new McpActiveService(transport, registry, sessionToken, authEnabled, message => runtime.Log("[MCP] {0}", message));
		}
	}
}

