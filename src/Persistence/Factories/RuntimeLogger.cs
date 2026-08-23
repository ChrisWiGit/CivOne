using CivOne.Governments;
using System;

namespace CivOne.Persistence.Factories
{
    public sealed class RuntimeLogger : ILogger
    {
        public void Log(string text, params object[] parameters)
        {
            RuntimeHandler.Runtime.Log(text, parameters);
        }
    }
}
