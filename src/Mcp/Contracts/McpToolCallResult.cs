using System.Text.Json.Serialization;

namespace CivOne.Mcp.Contracts
{
	/// <summary>
	/// MCP-compliant tool call result wrapper.
	/// VS Code expects <c>{ "content": [ { "type": "text", "text": "..." } ] }</c>.
	/// </summary>
	public sealed record McpToolCallResult(
		[property: JsonPropertyName("content")] McpContentItem[] Content
	)
	{
		/// <summary>Wraps a plain text message in the required content array.</summary>
		public static McpToolCallResult Text(string text)
			=> new(new[] { new McpContentItem("text", text) });
	}

	public sealed record McpContentItem(
		[property: JsonPropertyName("type")] string Type,
		[property: JsonPropertyName("text")] string Text
	);
}
