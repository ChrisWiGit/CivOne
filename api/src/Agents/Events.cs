using System;
using System.Collections.Generic;

namespace CivOne.Agents
{
	/// <summary>
	/// Defines high-level event kinds for the agent journal consumed via
	/// <see cref="IEventJournal.ReadSince(ulong)"/>.
	/// </summary>
	public enum AgentEventKind
	{
		Unknown = 0,
		TurnStarted = 1,
		UnitMoved = 2,
		TilesExplored = 3,
		CityProductionCompleted = 4,
		CityProductionChanged = 5,
		ResearchCompleted = 6,
		ResearchChanged = 7,
		UnitDestroyed = 8,
		CityLost = 9,
		UnitDisbanded = 10
	}

	/// <summary>
	/// Represents one journal event.
	/// Events are hints; current truth remains in <see cref="ITurnContext"/>.
	/// </summary>
	/// <param name="Sequence">The monotonically increasing sequence number.</param>
	/// <param name="Kind">The event kind.</param>
	/// <param name="EntityId">The related entity identifier if any.</param>
	/// <param name="Name">The related internal name if any.</param>
	/// <param name="Message">A diagnostic message if available.</param>
	public readonly record struct AgentEvent(
		ulong Sequence,
		AgentEventKind Kind,
		Guid? EntityId,
		string? Name,
		string? Message);

	/// <summary>
	/// Represents the result of a journal read operation.
	/// Cursor handling aligns with command sequence boundaries from <see cref="ICommandResult"/>.
	/// </summary>
	/// <param name="CursorExpired">Indicates whether the caller cursor is too old.</param>
	/// <param name="RequiresFullResync">Indicates whether the caller must resync from context.</param>
	/// <param name="FromSequence">The first sequence boundary of the result.</param>
	/// <param name="ToSequence">The last sequence boundary of the result.</param>
	/// <param name="Events">The returned events ordered by sequence.</param>
	public readonly record struct EventReadResult(
		bool CursorExpired,
		bool RequiresFullResync,
		ulong FromSequence,
		ulong ToSequence,
		IReadOnlyList<AgentEvent> Events);

	/// <summary>
	/// Provides read access to the turn event journal.
	/// </summary>
	public interface IEventJournal
	{
		/// <summary>
		/// Reads all newer events after the supplied sequence.
		/// </summary>
		/// <param name="sequence">The last processed sequence number.</param>
		/// <returns>The read result.</returns>
		EventReadResult ReadSince(ulong sequence);

		/// <summary>
		/// Gets the current journal sequence.
		/// </summary>
		ulong CurrentSequence { get; }
	}
}