using System;
using CivOne.Mcp.Contracts;
using CivOne.Mcp.Tools;
using CivOne.Mcp.Transport;

namespace CivOne.Mcp
{
	public sealed class McpActiveService : IMcpService
	{
		private readonly IMcpTransport _transport;
		private readonly IMcpToolRegistry _registry;
		private readonly string _sessionToken;
		private readonly bool _authEnabled;
		private readonly Action<string> _log;
		private bool _started;
		private bool _shutdownRequested;
		private bool _exitRequested;

		public void Start()
		{
			if (_started) return;
			_transport.Start();
			_started = true;
			if (_authEnabled)
			{
				// Write session token to stderr so launching process can read it.
				Console.Error.WriteLine($"MCP_SESSION_TOKEN={_sessionToken}");
				Console.Error.Flush();
			}
		}

		public void Process()
		{
			if (!_started) return;

			// VS Code closes stdin when stopping the server — treat as implicit exit.
			if (_transport.StdinClosed)
			{
				Stop();
				return;
			}

			const int maxRequestsPerTick = 4;
			for (int i = 0; i < maxRequestsPerTick; i++)
			{
				if (!_transport.TryRead(out McpRequest request)) return;

				McpResponse response = null;
				try
				{
					if (string.Equals(request.Method, "initialize", StringComparison.Ordinal))
					{
						response = HandleInitialize(request);
					}
					else if (string.Equals(request.Method, "initialized", StringComparison.Ordinal))
					{
						// Notification from client; intentionally ignored.
						response = null;
					}
					else if (_authEnabled && !string.Equals(request.SessionToken, _sessionToken, StringComparison.Ordinal))
					{
						response = McpResponse.Failure(request.Id, -32001, "Unauthorized", "Invalid or missing session token.");
					}
					else if (string.Equals(request.Method, "shutdown", StringComparison.Ordinal))
					{
						_shutdownRequested = true;
						response = McpResponse.Success(request.Id, new { });
					}
					else if (string.Equals(request.Method, "exit", StringComparison.Ordinal))
					{
						_exitRequested = true;
						response = null;
					}
					else if (_shutdownRequested)
					{
						response = McpResponse.Failure(request.Id, -32601, "Method not found", request.Method);
					}
					else if (!_registry.TryGet(request.Method, out IMcpToolHandler handler))
					{
						response = McpResponse.Failure(request.Id, -32601, "Method not found", request.Method);
					}
					else
					{
						response = handler.Handle(request);
					}
				}
				catch (Exception ex)
				{
					_log?.Invoke($"MCP request failed: {ex.Message}");
					response = McpResponse.Failure(request?.Id, -32603, "Internal error", ex.Message);
				}

				if (response != null && ShouldRespond(request))
				{
					_transport.Write(response);
				}

				if (_exitRequested)
				{
					Stop();
					return;
				}
			}
		}

		private static bool ShouldRespond(McpRequest request)
		{
			if (request?.Id == null)
				return false;

			if (request.Id is System.Text.Json.JsonElement jsonId)
			{
				return jsonId.ValueKind == System.Text.Json.JsonValueKind.String
					|| jsonId.ValueKind == System.Text.Json.JsonValueKind.Number
					|| jsonId.ValueKind == System.Text.Json.JsonValueKind.True
					|| jsonId.ValueKind == System.Text.Json.JsonValueKind.False;
			}

			return true;
		}

		private static McpResponse HandleInitialize(McpRequest request)
		{
			if (request.Params.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
				request.Params.ValueKind != System.Text.Json.JsonValueKind.Null &&
				request.Params.ValueKind != System.Text.Json.JsonValueKind.Object)
			{
				return McpResponse.Failure(request.Id, -32602, "Invalid params", "Initialize params must be an object or null.");
			}

			return McpResponse.Success(request.Id, new
			{
				capabilities = new
				{
					tools = new { }
				},
				serverInfo = new
				{
					name = "civone-mcp",
					version = "1.0.0"
				}
			});
		}

		public void Stop()
		{
			if (!_started) return;
			_transport.Stop();
			_started = false;
		}

		public void Dispose()
		{
			Stop();
			_transport.Dispose();
		}

		public McpActiveService(IMcpTransport transport, IMcpToolRegistry registry, string sessionToken, bool authEnabled, Action<string> log)
		{
			_transport = transport ?? throw new ArgumentNullException(nameof(transport));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_sessionToken = sessionToken ?? throw new ArgumentNullException(nameof(sessionToken));
			_authEnabled = authEnabled;
			_log = log;
		}
	}
}

