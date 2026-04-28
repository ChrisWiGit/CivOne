using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Mcp.Tools
{
	public sealed class McpToolRegistry : IMcpToolRegistry
	{
		private readonly Dictionary<string, IMcpToolHandler> _handlers;

		public IReadOnlyCollection<IMcpToolHandler> All => _handlers.Values;

		public bool TryGet(string method, out IMcpToolHandler handler)
		{
			handler = null;
			if (string.IsNullOrWhiteSpace(method)) return false;
			return _handlers.TryGetValue(method, out handler);
		}

		public McpToolRegistry(IEnumerable<IMcpToolHandler> handlers)
		{
			if (handlers == null) throw new ArgumentNullException(nameof(handlers));
			_handlers = handlers
				.GroupBy(x => x.Method, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		}
	}
}
