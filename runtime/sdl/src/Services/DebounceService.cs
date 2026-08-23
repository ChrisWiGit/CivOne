// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne
{
	/// <summary>
	/// Default implementation of <see cref="IDebounceService"/>.
	/// Stores one pending callback per key and executes callbacks on explicit polling.
	/// </summary>
	/// <remarks>
	/// Design notes:
	/// - Keyed debounce: the latest callback replaces previous callbacks for the same key.
	/// - No background timer/thread is used; caller controls execution by calling
	///   <see cref="ExecuteDueCallbacks"/> in the update loop.
	/// - <see cref="FlushPendingCallbacks"/> is intended for shutdown to avoid losing
	///   the last buffered write.
	/// </remarks>
	/// <remarks>
	/// Creates a debounce service with an injectable UTC clock.
	/// </remarks>
	/// <param name="clock">UTC time source used for due-time calculations.</param>
	/// <param name="log">Optional error logger for callback exceptions.</param>
	public sealed class DebounceService(IUtcClock clock, Action<string> log) : IDebounceService
	{
		private sealed class PendingCallback(DateTime dueAtUtc, Action callback)
		{
			public DateTime DueAtUtc { get; set; } = dueAtUtc;
			public Action Callback { get; set; } = callback;
		}

		private readonly IUtcClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
		private readonly Action<string> _log = log;
		private readonly Dictionary<string, PendingCallback> _pending = [];
		private readonly object _sync = new object();

		public void Debounce(string key, TimeSpan delay, Action callback)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Debounce key must be non-empty.", nameof(key));
			}

			ArgumentNullException.ThrowIfNull(callback);

			if (delay < TimeSpan.Zero)
			{
				delay = TimeSpan.Zero;
			}

			DateTime dueAt = _clock.UtcNow.Add(delay);
			lock (_sync)
			{
				_pending[key] = new PendingCallback(dueAt, callback);
			}
		}

		public void Cancel(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}

			lock (_sync)
			{
				_pending.Remove(key);
			}
		}

		public void ExecuteDueCallbacks()
		{
			DateTime now = _clock.UtcNow;
			List<KeyValuePair<string, PendingCallback>> dueEntries = [];
			List<string> dueKeys = [];

			lock (_sync)
			{
				foreach (KeyValuePair<string, PendingCallback> entry in _pending)
				{
					bool isCallbackDue = entry.Value.DueAtUtc <= now;
					if (isCallbackDue)
					{
						dueEntries.Add(entry);
						dueKeys.Add(entry.Key);
					}
				}

				foreach (string key in dueKeys)
				{
					_pending.Remove(key);
				}
			}

			foreach (KeyValuePair<string, PendingCallback> entry in dueEntries)
			{
				Execute(entry.Key, entry.Value.Callback);
			}
		}

		public void FlushPendingCallbacks()
		{
			List<KeyValuePair<string, PendingCallback>> allEntries = [];

			lock (_sync)
			{
				foreach (KeyValuePair<string, PendingCallback> entry in _pending)
				{
					allEntries.Add(entry);
				}
				_pending.Clear();
			}

			foreach (KeyValuePair<string, PendingCallback> entry in allEntries)
			{
				Execute(entry.Key, entry.Value.Callback);
			}
		}

		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions is necessary to ensure that failure in a debounce callback does not crash the application, and that any exceptions are logged appropriately.")]
		private void Execute(string key, Action callback)
		{
			try
			{
				callback();
			}
			catch (Exception ex)
			{
				_log?.Invoke($"Debounce callback for key '{key}' failed: {ex}");
			}
		}
	}
}
