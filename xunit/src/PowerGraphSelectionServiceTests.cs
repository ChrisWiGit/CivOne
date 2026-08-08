using System.Collections.Generic;
using System.Linq;
using CivOne.Services.Screen;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Tests for the civilization selection of the power graph.
	/// </summary>
	public class PowerGraphSelectionServiceTests
	{
		private readonly PowerGraphSelectionService _testee = new();

		private static int[] Candidates(int count) => [.. Enumerable.Range(1, count)];

		[Theory]
		[InlineData(7, false)]
		[InlineData(12, false)]
		[InlineData(13, true)]
		[InlineData(31, true)]
		public void RequiresSelectionOnlyAboveTheVisibleLimit(int playerCount, bool expected)
		{
			Assert.Equal(expected, _testee.RequiresSelection(Candidates(playerCount)));
		}

		[Fact]
		public void SmallGamesShowEveryPlayer()
		{
			int[] candidates = Candidates(12);

			IReadOnlyList<int> visible = _testee.GetVisiblePlayers(candidates, humanPlayerNumber: 1);

			Assert.Equal(candidates, visible);
		}

		[Fact]
		public void LargeGamesDefaultToTheFirstPlayersAndTheHumanPlayer()
		{
			IReadOnlyList<int> visible = _testee.GetVisiblePlayers(Candidates(31), humanPlayerNumber: 20);

			Assert.Equal([1, 2, 3, 4, 5, 6, 20], visible);
		}

		[Fact]
		public void SelectionIsCappedAtTheVisibleLimit()
		{
			int[] candidates = Candidates(31);
			_testee.GetVisiblePlayers(candidates, humanPlayerNumber: 1);

			// Five more fit next to the seven defaults, the sixth one is rejected.
			for (int playerNumber = 8; playerNumber <= 12; playerNumber++)
			{
				Assert.True(_testee.Toggle(playerNumber));
			}
			Assert.False(_testee.Toggle(13));

			Assert.Equal(12, _testee.SelectedCount);
			Assert.False(_testee.IsSelected(13));
			Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], _testee.GetVisiblePlayers(candidates, humanPlayerNumber: 1));
		}

		[Fact]
		public void DeselectingMakesRoomAgain()
		{
			int[] candidates = Candidates(31);
			_testee.GetVisiblePlayers(candidates, humanPlayerNumber: 1);

			Assert.True(_testee.Toggle(1));
			Assert.False(_testee.IsSelected(1));

			Assert.True(_testee.Toggle(31));
			Assert.Equal([2, 3, 4, 5, 6, 7, 31], _testee.GetVisiblePlayers(candidates, humanPlayerNumber: 1));
		}

		[Fact]
		public void ANewGameResetsTheSelection()
		{
			_testee.GetVisiblePlayers(Candidates(31), humanPlayerNumber: 1);
			_testee.Toggle(1);

			IReadOnlyList<int> visible = _testee.GetVisiblePlayers(Candidates(24), humanPlayerNumber: 1);

			Assert.Equal([1, 2, 3, 4, 5, 6, 7], visible);
		}
	}
}
