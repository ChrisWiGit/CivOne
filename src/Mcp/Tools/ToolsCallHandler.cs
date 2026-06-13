using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Tools
{
	/// <summary>
	/// Handles the standard MCP <c>tools/call</c> method.
	/// Dispatches to the real tool handler by tool name with the call's arguments as params.
	/// </summary>
	public sealed class ToolsCallHandler : IMcpToolHandler
	{
		private readonly Dictionary<string, IMcpToolHandler> _lookup;
		private readonly Action<string>? _logger;

		public string Method => "tools/call";

		// Not itself exposed in tools/list
		public ToolDefinition? Definition => null;

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (request.Params.ValueKind != JsonValueKind.Object)
				return McpResponse.Failure(request.Id, -32602, "Invalid params", "'params' must be an object with 'name' and 'arguments'.");

			if (!request.Params.TryGetProperty("name", out JsonElement nameElement))
				return McpResponse.Failure(request.Id, -32602, "Invalid params", "Property 'name' is required.");

			string? toolName = nameElement.GetString();
			if (string.IsNullOrWhiteSpace(toolName))
				return McpResponse.Failure(request.Id, -32602, "Invalid params", "Property 'name' must be a non-empty string.");

			if (!_lookup.TryGetValue(toolName, out IMcpToolHandler? handler))
				return McpResponse.Failure(request.Id, -32601, "Method not found", toolName);

			JsonElement arguments = request.Params.TryGetProperty("arguments", out JsonElement argsElement)
				? argsElement
				: default;

			_logger?.Invoke($"tools/call: {toolName} args={arguments}");

			McpRequest innerRequest = new(request.JsonRpc, request.Id, toolName, arguments, request.SessionToken);
			return handler.Handle(innerRequest);
		}

		public ToolsCallHandler(IEnumerable<IMcpToolHandler> realHandlers, Action<string>? logger = null)
		{
			ArgumentNullException.ThrowIfNull(realHandlers);
			_lookup = realHandlers
				.Where(h => h.Definition != null)
				.ToDictionary(h => h.Method, h => h, StringComparer.OrdinalIgnoreCase);
			_logger = logger;
		}
	}
}
