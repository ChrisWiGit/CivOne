using System.Text.Json.Serialization;

namespace CivOne.Mcp.Contracts
{
	public sealed record McpResponse(
		[property: JsonPropertyName("jsonrpc")] string JsonRpc,
		[property: JsonPropertyName("id")] object? Id,
		[property: JsonPropertyName("result")] object? Result,
		[property: JsonPropertyName("error")] McpError? Error
	)
	{
		public static McpResponse Success(object? id, object? result) => new("2.0", id, result, null);
		public static McpResponse Failure(object? id, int code, string message, string? details = null)
			=> new("2.0", id, null, new McpError(code, message, details));
	}

	public sealed record McpError(
		[property: JsonPropertyName("code")] int Code,
		[property: JsonPropertyName("message")] string Message,
		[property: JsonPropertyName("details")] string? Details
	);
}
