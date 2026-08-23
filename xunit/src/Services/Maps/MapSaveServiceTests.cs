// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using Xunit;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Tests for <see cref="MapSaveService.BuildStartPositions"/>, the Barbarian-skip filter that
	/// decides which civilizations' start positions get written into a saved <c>*.comap</c> file.
	/// </summary>
	/// <remarks>
	/// Derives from <see cref="src.TestsBase"/> to get a real <c>Common.Civilizations</c> list
	/// (needed for a genuine <see cref="Barbarian"/> instance to exercise the skip check against),
	/// combined with a <see cref="UnitTests.MockedMapEditor"/> so the start positions themselves stay
	/// fully isolated from the real <c>Map</c> singleton.
	/// </remarks>
	public class MapSaveServiceTests : src.TestsBase
	{
		[Fact]
		public void BuildStartPositionsSkipsBarbarians()
		{
			ICivilization barbarian = Common.Civilizations.Single(c => c is Barbarian);
			UnitTests.MockedMapEditor mapEditor = new();
			mapEditor.SeedStartPosition(Civilization.Barbarians, new MapLocation(1, 1));

			Dictionary<string, MapLocation>? actual = MapSaveService.BuildStartPositions([barbarian], mapEditor);

			Assert.Null(actual);
		}

		[Fact]
		public void BuildStartPositionsIncludesNonBarbarianCivilizationsWithStartPosition()
		{
			ICivilization romans = Common.Civilizations.Single(c => c.Id == (int)Civilization.Romans);
			UnitTests.MockedMapEditor mapEditor = new();
			mapEditor.SeedStartPosition(Civilization.Romans, new MapLocation(10, 12));

			Dictionary<string, MapLocation>? actual = MapSaveService.BuildStartPositions([romans], mapEditor);

			Assert.NotNull(actual);
			MapLocation location = Assert.Contains(nameof(Civilization.Romans), actual);
			Assert.Equal(10u, location.X);
			Assert.Equal(12u, location.Y);
		}

		[Fact]
		public void BuildStartPositionsExcludesCivilizationsWithoutStartPosition()
		{
			ICivilization romans = Common.Civilizations.Single(c => c.Id == (int)Civilization.Romans);
			UnitTests.MockedMapEditor mapEditor = new();

			Dictionary<string, MapLocation>? actual = MapSaveService.BuildStartPositions([romans], mapEditor);

			Assert.Null(actual);
		}

		[Fact]
		public void BuildStartPositionsReturnsNullWhenCivilizationsIsEmpty()
		{
			UnitTests.MockedMapEditor mapEditor = new();

			Dictionary<string, MapLocation>? actual = MapSaveService.BuildStartPositions([], mapEditor);

			Assert.Null(actual);
		}
	}
}
