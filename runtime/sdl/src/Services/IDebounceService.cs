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
	/// Schedules callbacks so that only the latest callback per key executes after a quiet period.
	/// </summary>
	/// <remarks>
	/// Typical usage in a frame/update loop:
	/// <code>
	/// _debounceService.Debounce("window-size", TimeSpan.FromSeconds(1), () =&gt;
	/// {
	///     Settings.WindowWidth = width;
	///     Settings.WindowHeight = height;
	/// });
	///
	/// // Called each update tick:
	/// _debounceService.ExecuteDueCallbacks();
	///
	/// // Called on quit:
	/// _debounceService.FlushPendingCallbacks();
	/// </code>
	/// </remarks>
	public interface IDebounceService
	{
		/// <summary>
		/// Schedules or replaces the callback for <paramref name="key"/>.
		/// Only the latest callback for that key is kept.
		/// </summary>
		void Debounce(string key, TimeSpan delay, Action callback);

		/// <summary>
		/// Removes a pending callback for <paramref name="key"/>, if any.
		/// </summary>
		void Cancel(string key);

		/// <summary>
		/// Executes callbacks whose debounce delay has elapsed.
		/// </summary>
		void ExecuteDueCallbacks();

		/// <summary>
		/// Executes all pending callbacks immediately.
		/// Intended for controlled shutdown.
		/// </summary>
		void FlushPendingCallbacks();
	}
}
