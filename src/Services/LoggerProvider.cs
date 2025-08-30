namespace CivOne.Services
{
	public static class LoggerProvider
	{
		public static ILogger GetLogger()
		{
			return new LoggerImpl(RuntimeHandler.Runtime);
		}
	}
}