using System.Text.Json.Serialization;

namespace CivOne.Mcp.Tools
{
	/// <summary>
	/// Describes an MCP tool for use in the tools/list response.
	/// </summary>
	public sealed record ToolDefinition(
		[property: JsonPropertyName("name")] string Name,
		[property: JsonPropertyName("description")] string Description,
		[property: JsonPropertyName("inputSchema")] object InputSchema
	);
}
