using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Tools
{
	public interface IMcpToolHandler
	{
		string Method { get; }

		/// <summary>Tool definition for the tools/list response. Null = not exposed.</summary>
		ToolDefinition? Definition { get; }

		McpResponse Handle(McpRequest request);
	}
}
