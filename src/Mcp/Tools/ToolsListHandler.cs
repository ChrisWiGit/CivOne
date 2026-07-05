using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Tools
{
	/// <summary>
	/// Handles the standard MCP <c>tools/list</c> method.
	/// Returns definitions of all exposed game tools.
	/// </summary>
	public sealed class ToolsListHandler : IMcpToolHandler
	{
		private readonly IReadOnlyList<ToolDefinition> _definitions;

		public string Method => "tools/list";

		// Not itself exposed in the listing
		public ToolDefinition? Definition => null;

		public McpResponse Handle(McpRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);
			return McpResponse.Success(request.Id, new { tools = _definitions });
		}

		public ToolsListHandler(IEnumerable<ToolDefinition> definitions)
		{
			ArgumentNullException.ThrowIfNull(definitions);
			_definitions = [.. definitions];
		}
	}
}
