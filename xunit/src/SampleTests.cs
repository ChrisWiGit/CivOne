// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	public class SampleTests : TestsBase
	{
		[Fact]
		public void CityNamesCount()
        {
            int expectedCount = 256;
            int count = Common.AllCityNames.Count();
            bool result = (count == expectedCount);

            Assert.True(result, $"Common.AllCityNames should have {expectedCount} entries, returns {count}.");
        }

		[Fact]
		public void PaletteColourCount()
		{
            using var defaultPalette = Common.DefaultPalette;
            
            int expectedCount = 256;
            int count = defaultPalette.Length;
            bool result = count == expectedCount;

            Assert.True(result, $"Common.DefaultPalette should have {expectedCount} entries, returns {count}.");
        }

        [Fact]
        public void Distance()
        {
            for (int dx = -1; dx < 2; dx++)
            for (int dy = -1; dy < 2; dy++)
            {
                var val = Common.Distance(10, 10, 10+dx, 10+dy);
                if (dx == 0 && dy == 0)
                    Assert.Equal(0,val);
                else
                    Assert.Equal(1, val);

            }
        }
	}
}