namespace CivOne.Mcp.Automation
{
	public interface IMcpScreenshotRoutine
	{
		McpScreenshotResult CaptureFull(string sessionId, bool includeCursor);
		McpScreenshotResult CaptureRegion(string sessionId, int x, int y, int width, int height, bool includeCursor);
	}
}
