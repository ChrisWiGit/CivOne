#nullable enable
// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne
{
	/// <summary>
	/// Creates <see cref="IDebounceService"/> instances.
	/// Centralizes debounce implementation selection so future variants
	/// (e.g. timer-based or multithreaded) can be swapped without touching callers.
	/// Current implementation is a simple single-threaded service that relies on regular calls to ExecuteDueCallbacks() from the main loop.
	/// If no clock is provided, uses SystemUtcClock which simply returns DateTime.UtcNow. 
	/// This indirection allows tests to use a mock clock for deterministic timing.
	/// </summary>
	public static class DebounceServiceFactory
	{
		public static IDebounceService Create(Action<string> log, IUtcClock? clock = null)
			=> new DebounceService(clock ?? new SystemUtcClock(), log);
	}
}
