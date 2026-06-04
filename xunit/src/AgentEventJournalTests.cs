using CivOne.Agents;
using Xunit;

namespace CivOne.UnitTests
{
	public class AgentEventJournalTests
	{
		[Fact]
		public void ReadSince_ReturnsEventsInAscendingOrder()
		{
			// Arrange
			RingEventJournal journal = new();
			journal.Append(AgentEventKind.TurnStarted, null, null, "start");
			journal.Append(AgentEventKind.UnitMoved, null, null, "move");
			journal.Append(AgentEventKind.ResearchChanged, null, "Alphabet", "research");

			// Act
			EventReadResult actual = journal.ReadSince(0);

			// Assert
			Assert.False(actual.CursorExpired);
			Assert.False(actual.RequiresFullResync);
			Assert.Equal(1UL, actual.FromSequence);
			Assert.Equal(3UL, actual.ToSequence);
			Assert.Equal(3, actual.Events.Count);
			Assert.Equal(1UL, actual.Events[0].Sequence);
			Assert.Equal(2UL, actual.Events[1].Sequence);
			Assert.Equal(3UL, actual.Events[2].Sequence);
		}

		[Fact]
		public void ReadSince_WhenCursorTooOld_ReturnsCursorExpiredWithResync()
		{
			// Arrange
			RingEventJournal journal = new();
			for (int i = 0; i < 110; i++)
			{
				journal.Append(AgentEventKind.UnitMoved, null, null, "evt");
			}

			// Act
			EventReadResult actual = journal.ReadSince(0);

			// Assert
			Assert.True(actual.CursorExpired);
			Assert.True(actual.RequiresFullResync);
			Assert.Equal(100, actual.Events.Count);
			Assert.Equal(11UL, actual.Events[0].Sequence);
			Assert.Equal(110UL, actual.Events[99].Sequence);
		}

		[Fact]
		public void ReadSince_ReturnsOnlyEventsAfterCursor()
		{
			// Arrange
			RingEventJournal journal = new();
			for (int i = 0; i < 8; i++)
			{
				journal.Append(AgentEventKind.UnitMoved, null, null, "evt");
			}

			// Act
			EventReadResult actual = journal.ReadSince(5);

			// Assert
			Assert.False(actual.CursorExpired);
			Assert.False(actual.RequiresFullResync);
			Assert.Equal(3, actual.Events.Count);
			Assert.Equal(6UL, actual.FromSequence);
			Assert.Equal(8UL, actual.ToSequence);
			Assert.Equal(6UL, actual.Events[0].Sequence);
			Assert.Equal(8UL, actual.Events[2].Sequence);
		}

		[Fact]
		public void ReadSince_WhenCursorEqualsCurrentSequence_ReturnsNoEvents()
		{
			// Arrange
			RingEventJournal journal = new();
			for (int i = 0; i < 3; i++)
			{
				journal.Append(AgentEventKind.UnitMoved, null, null, "evt");
			}

			// Act
			EventReadResult actual = journal.ReadSince(3);

			// Assert
			Assert.False(actual.CursorExpired);
			Assert.False(actual.RequiresFullResync);
			Assert.Empty(actual.Events);
			Assert.Equal(3UL, actual.FromSequence);
			Assert.Equal(3UL, actual.ToSequence);
		}

		[Fact]
		public void ReadSince_WhenCursorEqualsOldestMinusOne_ReturnsAllWithoutResync()
		{
			// Arrange
			RingEventJournal journal = new();
			for (int i = 0; i < 100; i++)
			{
				journal.Append(AgentEventKind.UnitMoved, null, null, "evt");
			}

			// Act
			EventReadResult actual = journal.ReadSince(0);

			// Assert
			Assert.False(actual.CursorExpired);
			Assert.False(actual.RequiresFullResync);
			Assert.Equal(100, actual.Events.Count);
			Assert.Equal(1UL, actual.Events[0].Sequence);
			Assert.Equal(100UL, actual.Events[99].Sequence);
		}
	}
}
