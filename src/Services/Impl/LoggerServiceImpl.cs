using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne
{
	public class LoggerServiceImpl(IRuntime runtime) : ILoggerService
	{
		public void Log(string text, params object[] parameters)
		{
			runtime.Log(text, parameters);
		}
	}
}