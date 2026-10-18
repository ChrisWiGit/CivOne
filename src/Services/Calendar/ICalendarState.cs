using System.Collections.Generic;

namespace CivOne.Services.Calendar
{
	/// <summary>
	/// Per-game calendar history: the turns in which the year advanced by 1 instead of 2
	/// because a spaceship was in flight between 1750 and 1850.
	/// </summary>
	/// <remarks>
	/// The year of every turn, also of past turns, follows from the turn number and this set.
	/// The set is empty in every game without a spaceship launched before 1850.
	/// </remarks>
	public interface ICalendarState
	{
		/// <summary>
		/// Gets the accelerated turns in ascending order.
		/// </summary>
		IReadOnlyCollection<ushort> AcceleratedTurns { get; }

		/// <summary>
		/// Checks whether the year advanced by 1 when <paramref name="turn"/> began.
		/// </summary>
		/// <param name="turn">The turn to check.</param>
		/// <returns><c>true</c> if the turn is accelerated.</returns>
		bool IsAccelerated(ushort turn);

		/// <summary>
		/// Records that the year advanced by 1 when <paramref name="turn"/> began.
		/// </summary>
		/// <param name="turn">The new turn.</param>
		void MarkAccelerated(ushort turn);

		/// <summary>
		/// Replaces the history, e.g. after loading a saved game.
		/// </summary>
		/// <param name="turns">The accelerated turns; duplicates are ignored.</param>
		void Restore(IEnumerable<ushort> turns);
	}
}
