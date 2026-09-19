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