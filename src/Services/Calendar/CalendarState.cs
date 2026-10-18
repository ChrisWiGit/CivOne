using System;
using System.Collections.Generic;

namespace CivOne.Services.Calendar
{
	/// <summary>
	/// In-memory <see cref="ICalendarState"/>, owned by the running game and persisted with it.
	/// </summary>
	public sealed class CalendarState : ICalendarState
	{
		private readonly SortedSet<ushort> _acceleratedTurns = [];

		/// <inheritdoc />
		public IReadOnlyCollection<ushort> AcceleratedTurns => _acceleratedTurns;

		/// <inheritdoc />
		public bool IsAccelerated(ushort turn) => _acceleratedTurns.Contains(turn);

		/// <inheritdoc />
		public void MarkAccelerated(ushort turn) => _acceleratedTurns.Add(turn);

		/// <inheritdoc />
		public void Restore(IEnumerable<ushort> turns)
		{
			ArgumentNullException.ThrowIfNull(turns);

			_acceleratedTurns.Clear();
			_acceleratedTurns.UnionWith(turns);
		}
	}
}
