using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.Calendar
{
	/// <summary>
	/// Rebuilds the calendar history from an SVE file, which stores only the current turn and year.
	/// </summary>
	/// <remarks>
	/// Each 1-year step in the 2-year phase (1750-1850) makes the year fall one behind the plain formula.
	/// The number of such steps is therefore known exactly, but not in which turns they happened.
	/// Before 1850 the most recent turns are assumed (a ship in flight right now), after 1850 the turns right before
	/// the calendar reached 1850. The current year is always exact; only the years shown for past turns can differ.
	/// See <c>docs/plan-calendar.impl.md</c>, Phase 5.
	/// </remarks>
	internal sealed class SveCalendarDelegate
	{
		private const ushort FirstTwoYearTurn = 350;
		private const int FirstOneYearYear = 1850;
		private const int MaxAcceleratedTurns = 100;

		/// <summary>
		/// Finds accelerated turns that make <paramref name="turn"/> fall on <paramref name="year"/>.
		/// </summary>
		/// <param name="turn">The saved turn.</param>
		/// <param name="year">The saved year.</param>
		/// <returns>The accelerated turns; empty if the plain formula already matches or no history fits.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public IReadOnlyCollection<ushort> Rebuild(ushort turn, int year)
		{
			CalendarState state = new();
			IGameCalendarService calendar = GameCalendarServiceFactory.Create(state);
			if (turn <= FirstTwoYearTurn || calendar.TurnToYear(turn) == year)
			{
				return [];
			}

			for (int count = 1; count <= Math.Min(MaxAcceleratedTurns, turn - FirstTwoYearTurn); count++)
			{
				state.Restore(Candidates(turn, year, count));
				if (calendar.TurnToYear(turn) == year)
				{
					return [.. state.AcceleratedTurns];
				}
			}

			return [];
		}

		private static IEnumerable<ushort> Candidates(ushort turn, int year, int count)
		{
			if (year < FirstOneYearYear)
			{
				return Enumerable.Range(turn - count + 1, count).Select(t => (ushort)t);
			}

			// With n accelerated turns, 1850 is reached at turn 400 + ceil(n / 2); the steps lie right before it.
			int lastTwoYearTurn = 400 + ((count + 1) / 2);
			return Enumerable.Range(lastTwoYearTurn - count + 1, count).Select(t => (ushort)t);
		}
	}
}
