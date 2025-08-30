using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne
{
	public interface ILogger
	{
		void Log(string text, params object[] parameters);
	}
}