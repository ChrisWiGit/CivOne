using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CivOne.Services.Sorting
{
	/// <summary>
	/// Tests for <see cref="NaturalSortService"/>.
	/// </summary>
	public sealed class NaturalSortServiceTests
	{
		private readonly NaturalSortService _testee = new();

		[Fact]
		public void OrdersEmbeddedNumbersByValueNotLexicographically()
		{
			List<string> input = ["map10", "map2", "map1", "map0", "map99"];

			List<string> actual = [.. input.Order(_testee)];

			Assert.Equal(["map0", "map1", "map2", "map10", "map99"], actual);
		}

		[Fact]
		public void OrdersPlainTextCaseInsensitively()
		{
			List<string> input = ["zulu", "Alpha", "mike"];

			List<string> actual = [.. input.Order(_testee)];

			Assert.Equal(["Alpha", "mike", "zulu"], actual);
		}

		[Fact]
		public void SameMagnitudeWithLeadingZerosBreaksTiesByLength()
		{
			// "007" and "7" are numerically equal, so the shorter raw name ("map7") sorts first.
			Assert.True(_testee.Compare("map007", "map7") > 0);
			Assert.True(_testee.Compare("map7", "map007") < 0);
		}

		[Fact]
		public void ShorterStringSortsBeforeLongerStringWithSamePrefix()
		{
			Assert.True(_testee.Compare("map1", "map10") < 0);
		}

		[Fact]
		public void NullPrecedesNonNullValue()
		{
			Assert.True(_testee.Compare(null, "map1") < 0);
			Assert.True(_testee.Compare("map1", null) > 0);
			Assert.Equal(0, _testee.Compare(null, null));
		}
	}
}
