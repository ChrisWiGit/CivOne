using System;
using System.Text.Json;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Transport
{
	public sealed class JsonRpcProtocolSerializer : IMcpProtocolSerializer
	{
		public bool TryParse(string raw, out McpRequest request, out McpResponse parseErrorResponse)
		{
			request = null;
			parseErrorResponse = null;

			if (string.IsNullOrWhiteSpace(raw))
			{
				parseErrorResponse = McpResponse.Failure(null, -32700, "Parse error", "Empty request.");
				return false;
			}

			try
			{
				using JsonDocument document = JsonDocument.Parse(raw);
				JsonElement root = document.RootElement;
				string jsonRpc = root.TryGetProperty("jsonrpc", out JsonElement jsonRpcValue)
					? jsonRpcValue.GetString()
					: "2.0";
				object id = root.TryGetProperty("id", out JsonElement idValue)
					? idValue.Clone()
					: null;
				if (id is JsonElement jsonId && !IsValidRequestId(jsonId))
				{
					parseErrorResponse = McpResponse.Failure(null, -32600, "Invalid request", "Property 'id' must be a JSON scalar.");
					return false;
				}
				string method = root.TryGetProperty("method", out JsonElement methodValue)
					? methodValue.GetString()
					: null;
				JsonElement parameters = root.TryGetProperty("params", out JsonElement paramsValue)
					? paramsValue.Clone()
					: default;
				string sessionToken = root.TryGetProperty("sessionToken", out JsonElement tokenValue)
					? tokenValue.GetString()
					: null;

				if (string.IsNullOrWhiteSpace(method))
				{
					parseErrorResponse = McpResponse.Failure(id, -32600, "Invalid request", "Property 'method' is required.");
					return false;
				}

				request = new McpRequest(jsonRpc ?? "2.0", id, method, parameters, sessionToken);
				return true;
			}
			catch (Exception ex)
			{
				parseErrorResponse = McpResponse.Failure(null, -32700, "Parse error", ex.Message);
				return false;
			}
		}

		public string Serialize(McpResponse response)
		{
			if (response == null) throw new ArgumentNullException(nameof(response));
			return JsonSerializer.Serialize(response);
		}

		private static bool IsValidRequestId(JsonElement id)
		{
			return id.ValueKind == JsonValueKind.String
				|| id.ValueKind == JsonValueKind.Number
				|| id.ValueKind == JsonValueKind.True
				|| id.ValueKind == JsonValueKind.False;
		}
	}
}
