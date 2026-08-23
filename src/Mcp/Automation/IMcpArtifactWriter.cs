namespace CivOne.Mcp.Automation
{
	public interface IMcpArtifactWriter
	{
		string WriteArtifact(string sessionId, string extension, byte[] bytes);
	}
}
