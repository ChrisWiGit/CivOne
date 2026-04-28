using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Transport
{
	public interface IMcpProtocolSerializer
	{
		bool TryParse(string raw, out McpRequest request, out McpResponse parseErrorResponse);
		string Serialize(McpResponse response);
	}
}
