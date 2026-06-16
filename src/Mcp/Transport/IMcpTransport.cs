using System;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Transport
{
	public interface IMcpTransport : IDisposable
	{
		void Start();
		bool TryRead(out McpRequest? request);
		void Write(McpResponse response);
		void StopService();
		/// <summary>True when the host closed stdin (EOF) — server should exit.</summary>
		bool StdinClosed { get; }
	}
}
