namespace CivOne.Mcp.Automation
{
	public sealed class RuntimeHandlerGameTickProvider : IMcpGameTickProvider
	{
		public uint CurrentTick => RuntimeHandler.CurrentGameTick;
	}
}
