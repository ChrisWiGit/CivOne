using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne
{
	public interface ILoggerService
	{
		void Log(string text, params object[] parameters);
	}
}