// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Persistence.Model;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Tests for the terrain-editor start-position mutators on <see cref="Map"/>
	/// (<c>Map.SetStartPosition</c>, <c>Map.RemoveStartPosition</c> in <c>Map.Editor.cs</c>),
	/// exercised through the real <see cref="Map.Instance"/> singleton (see <see cref="src.TestsBase"/>)
	/// since they are thin, dictionary-backed wrappers around its own state.
	/// </summary>
	public class MapEditorStartPositionTests : src.TestsBase
	{
		private static MockedICivilization Romans() => MockedICivilization.Mock(1)[0]; // Id 1 -> Civilization.Romans

		[Fact]
		public void SetStartPositionStoresLocationRetrievableViaTryGetStartPosition()
		{
			Map.Instance.SetStartPosition(Civilization.Romans, new MapLocation(10, 12));

			bool found = Map.Instance.TryGetStartPosition(Romans(), out MapLocation? actual);

			Assert.True(found);
			Assert.Equal(10u, actual!.X);
			Assert.Equal(12u, actual.Y);
		}

		[Fact]
		public void SetStartPositionEnablesFixedStartPositions()
		{
			Map.Instance.SetStartPosition(Civilization.Romans, new MapLocation(10, 12));

			Assert.True(Map.Instance.FixedStartPositions);
		}

		[Fact]
		public void SetStartPositionCopiesLocationInsteadOfAliasing()
		{
			MapLocation location = new(10, 12);

			Map.Instance.SetStartPosition(Civilization.Romans, location);
			Map.Instance.TryGetStartPosition(Romans(), out MapLocation? stored);

			Assert.NotSame(location, stored);
		}

		[Fact]
		public void RemoveStartPositionClearsLocation()
		{
			Map.Instance.SetStartPosition(Civilization.Romans, new MapLocation(10, 12));

			Map.Instance.RemoveStartPosition(Civilization.Romans);

			bool found = Map.Instance.TryGetStartPosition(Romans(), out _);
			Assert.False(found);
		}

		[Fact]
		public void RemoveStartPositionDisablesFixedStartPositionsWhenLastEntryRemoved()
		{
			Map.Instance.SetStartPosition(Civilization.Romans, new MapLocation(10, 12));

			Map.Instance.RemoveStartPosition(Civilization.Romans);

			Assert.False(Map.Instance.FixedStartPositions);
		}

		[Fact]
		public void RemoveStartPositionKeepsFixedStartPositionsWhenOtherEntriesRemain()
		{
			Map.Instance.SetStartPosition(Civilization.Romans, new MapLocation(10, 12));
			Map.Instance.SetStartPosition(Civilization.Babylonians, new MapLocation(20, 22));

			Map.Instance.RemoveStartPosition(Civilization.Romans);

			Assert.True(Map.Instance.FixedStartPositions);
		}

		[Fact]
		public void RemoveStartPositionForUnknownCivilizationIsNoOp()
		{
			Map.Instance.RemoveStartPosition(Civilization.Romans);

			Assert.False(Map.Instance.FixedStartPositions);
		}
	}
}
