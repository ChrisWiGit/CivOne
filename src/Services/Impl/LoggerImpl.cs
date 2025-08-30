using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne
{
	public class LoggerImpl(IRuntime runtime) : ILogger
	{
		public void Log(string text, params object[] parameters)
		{
			runtime.Log(text, parameters);
		}
	}
}