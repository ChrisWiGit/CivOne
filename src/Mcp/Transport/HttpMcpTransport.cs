using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Transport
{
	public sealed class HttpMcpTransport : IMcpTransport
	{
		private readonly IMcpProtocolSerializer _serializer;
		private readonly int _requestTimeoutMs;
		private readonly Action<string> _log;
		private readonly ConcurrentQueue<McpRequest> _queue = new();
		private readonly ConcurrentDictionary<string, BlockingCollection<McpResponse>> _pendingById = new(StringComparer.Ordinal);
		private readonly HttpListener _listener = new();
		private readonly string _prefix;

		private Thread? _readThread;
		private bool _running;

		public bool StdinClosed => false;

		public bool TryRead(out McpRequest? request) => _queue.TryDequeue(out request);

		public void Write(McpResponse response)
		{
			if (response == null)
				return;

			string? key = BuildRequestIdKey(response.Id);
			if (string.IsNullOrEmpty(key))
				return;

			if (_pendingById.TryGetValue(key, out BlockingCollection<McpResponse>? waiter))
			{
				waiter.TryAdd(response);
				return;
			}

			_log?.Invoke($"MCP HTTP response dropped: no pending waiter for id={key}");
		}

		public void Start()
		{
			if (_running)
				return;

			_listener.Prefixes.Add(_prefix);
			_listener.Start();
			_running = true;
			_readThread = new Thread(ReadLoop)
			{
				IsBackground = true,
				Name = "MCP-HTTP"
			};
			_readThread.Start();
		}

		public void StopService()
		{
			if (!_running)
				return;

			_running = false;
			try
			{
				_listener.Stop();
				_listener.Close();
			}
			catch (Exception ex)
			{
				_log?.Invoke($"MCP HTTP stop failed: {ex.Message}");
			}

			foreach (var pair in _pendingById)
			{
				pair.Value.CompleteAdding();
			}
		}

		private void ReadLoop()
		{
			while (_running)
			{
				HttpListenerContext context;
				try
				{
					context = _listener.GetContext();
				}
				catch (ObjectDisposedException)
				{
					return;
				}
				catch (HttpListenerException) when (!_running)
				{
					return;
				}
				catch (Exception ex)
				{
					_log?.Invoke($"MCP HTTP listener failed: {ex.Message}");
					continue;
				}

				ThreadPool.QueueUserWorkItem(_ => ProcessContext(context));
			}
		}

		private void ProcessContext(HttpListenerContext context)
		{
			try
			{
				if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
				{
					WriteStatus(context.Response, 405, "Only POST is supported.");
					return;
				}

				string payload;
				using (StreamReader reader = new(context.Request.InputStream, Encoding.UTF8, true, 1024, leaveOpen: true))
				{
					payload = reader.ReadToEnd();
				}

				if (!_serializer.TryParse(payload, out McpRequest? request, out McpResponse? parseError))
				{
					WriteJson(context.Response, parseError ?? McpResponse.Failure(null, -32700, "Parse error", "Invalid JSON."));
					return;
				}

				if (request!.Id == null)
				{
					_queue.Enqueue(request);
					context.Response.StatusCode = 202;
					context.Response.Close();
					return;
				}

				string? key = BuildRequestIdKey(request.Id);
				if (string.IsNullOrEmpty(key))
				{
					WriteJson(context.Response, McpResponse.Failure(request.Id, -32600, "Invalid request", "Property 'id' must be a JSON scalar."));
					return;
				}

				using BlockingCollection<McpResponse> waiter = new(1);
				if (!_pendingById.TryAdd(key, waiter))
				{
					WriteJson(context.Response, McpResponse.Failure(request.Id, -32600, "Invalid request", "Duplicate in-flight request id."));
					return;
				}

				try
				{
					_queue.Enqueue(request);
					if (waiter.TryTake(out McpResponse? response, _requestTimeoutMs))
					{
						WriteJson(context.Response, response);
					}
					else
					{
						WriteJson(context.Response, McpResponse.Failure(request.Id, -32000, "Timeout", "MCP request timed out waiting for response."));
					}
				}
				finally
				{
					_pendingById.TryRemove(key, out _);
				}
			}
			catch (Exception ex)
			{
				_log?.Invoke($"MCP HTTP request handling failed: {ex.Message}");
				try
				{
					WriteStatus(context.Response, 500, "Internal server error.");
				}
				catch
				{
					// Response may already be closed.
				}
			}
		}

		private void WriteJson(HttpListenerResponse response, McpResponse payload)
		{
			string json = _serializer.Serialize(payload);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			response.StatusCode = 200;
			response.ContentType = "application/json";
			response.ContentEncoding = Encoding.UTF8;
			response.ContentLength64 = bytes.LongLength;
			response.OutputStream.Write(bytes, 0, bytes.Length);
			response.OutputStream.Flush();
			response.Close();
		}

		private static void WriteStatus(HttpListenerResponse response, int statusCode, string message)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
			response.StatusCode = statusCode;
			response.ContentType = "text/plain";
			response.ContentEncoding = Encoding.UTF8;
			response.ContentLength64 = bytes.LongLength;
			response.OutputStream.Write(bytes, 0, bytes.Length);
			response.OutputStream.Flush();
			response.Close();
		}

		private static string? BuildRequestIdKey(object? id)
		{
			if (id == null)
				return null;

			if (id is JsonElement jsonId)
			{
				return jsonId.ValueKind switch
				{
					JsonValueKind.String => $"s:{jsonId.GetString()}",
					JsonValueKind.Number => $"n:{jsonId.GetRawText()}",
					JsonValueKind.True => "b:true",
					JsonValueKind.False => "b:false",
					_ => null
				};
			}

			return $"o:{JsonSerializer.Serialize(id)}";
		}

		public void Dispose() => StopService();

		public HttpMcpTransport(IMcpProtocolSerializer serializer, int port, int requestTimeoutMs, Action<string> log)
		{
			if (port <= 0 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port));

			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_requestTimeoutMs = Math.Max(1000, requestTimeoutMs);
			_log = log;
			_prefix = $"http://127.0.0.1:{port}/mcp/";
		}
	}
}