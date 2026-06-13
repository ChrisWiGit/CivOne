using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using CivOne.Mcp.Contracts;

namespace CivOne.Mcp.Transport
{
	public sealed class StdioMcpTransport : IMcpTransport
	{
		private readonly IMcpProtocolSerializer _serializer;
		private readonly Action<string> _log;
		private readonly ConcurrentQueue<McpRequest> _queue = new();
		private readonly object _writeLock = new();
		private Thread? _readThread;
		private bool _running;

		/// <summary>True when stdin reached EOF — the host process closed the pipe.</summary>
		public bool StdinClosed { get; private set; }

		public bool TryRead(out McpRequest? request) => _queue.TryDequeue(out request);

		public void Write(McpResponse response)
		{
			if (response == null) return;
			string payload = _serializer.Serialize(response);
			lock (_writeLock)
			{
				Console.Out.WriteLine(payload);
				Console.Out.Flush();
			}
		}

		public void Start()
		{
			if (_running) return;
			_running = true;
			_readThread = new Thread(ReadLoop)
			{
				IsBackground = true,
				Name = "MCP-STDIO"
			};
			_readThread.Start();
		}

		public void Stop()
		{
			_running = false;
		}

		private void ReadLoop()
		{
			Stream stdin = Console.OpenStandardInput();
			using StreamReader reader = new StreamReader(stdin, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

			while (_running)
			{
				string? payload = ReadMessage(reader);
				if (payload == null)
				{
					if (reader.EndOfStream)
					{
						// stdin was closed by the host — treat as implicit exit.
						StdinClosed = true;
						_running = false;
						return;
					}
					Thread.Sleep(5);
					continue;
				}

				if (_serializer.TryParse(payload, out McpRequest? request, out McpResponse? error))
				{
					if (request != null)
					{
						_queue.Enqueue(request);
					}
					continue;
				}

				if (error != null)
				{
					Write(error);
				}
				else
				{
					_log?.Invoke("MCP parser failed without error payload.");
				}
			}
		}

		private static string? ReadMessage(StreamReader reader)
		{
			// MCP stdio framing (Content-Length) with fallback for single-line JSON payloads.
			string? firstLine = reader.ReadLine();
			if (firstLine == null) return null;

			if (firstLine.StartsWith('{'))
				return firstLine;

			int contentLength = 0;
			string? line = firstLine;

			do
			{
				if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
				{
					string value = line.Substring("Content-Length:".Length).Trim();
					if (int.TryParse(value, out int parsedLength))
						contentLength = parsedLength;
				}

				line = reader.ReadLine();
			}
			while (line != null && line.Length > 0);

			if (contentLength <= 0)
				return null;

			char[] buffer = new char[contentLength];
			int read = 0;
			while (read < contentLength)
			{
				int n = reader.Read(buffer, read, contentLength - read);
				if (n <= 0) break;
				read += n;
			}

			return read == contentLength ? new string(buffer) : null;
		}

		public void Dispose() => Stop();

		public StdioMcpTransport(IMcpProtocolSerializer serializer, Action<string> log)
		{
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_log = log;
		}
	}
}
