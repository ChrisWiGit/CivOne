using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Mcp.Contracts
{
	public sealed record McpRequest(
		[property: JsonPropertyName("jsonrpc")] string JsonRpc,
		[property: JsonPropertyName("id")] object Id,
		[property: JsonPropertyName("method")] string Method,
		[property: JsonPropertyName("params")] JsonElement Params,
		[property: JsonPropertyName("sessionToken")] string SessionToken
	);
}
