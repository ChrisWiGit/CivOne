namespace CivOne.Services
{
	public static class LoggerProvider
	{
		public static ILoggerService GetLogger()
		{
			return new LoggerServiceImpl(RuntimeHandler.Runtime);
		}
	}
}