using System;

namespace CivOne.Mcp.Automation
{
	public sealed record McpScreenshotResult(
		string SessionId,
		uint GameTick,
		DateTime CapturedAtUtc,
		int Width,
		int Height,
		string Format,
		string ArtifactPath
	);
}
