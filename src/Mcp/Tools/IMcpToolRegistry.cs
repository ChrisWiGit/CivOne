namespace CivOne.Mcp.Tools
{
	public interface IMcpToolRegistry
	{
		bool TryGet(string method, out IMcpToolHandler? handler);
		System.Collections.Generic.IReadOnlyCollection<IMcpToolHandler> All { get; }
	}
}
