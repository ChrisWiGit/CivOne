namespace CivOne.Persistence.Model
{
	public class NoOpLogger : ILogger
	{
		public void Log(string text, params object[] parameters)
		{
		}
	}
}
