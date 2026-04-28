using System;

namespace CivOne.Mcp.Contracts
{
	public interface IMcpService : IDisposable
	{
		void Start();
		void Process();
		void Stop();
	}
}
