using System.Collections.Generic;
using CivOne.Services.Diplomacy;
using Xunit;

namespace CivOne.UnitTests.Services.Diplomacy
{
	public class SveDiplomacyDelegateTests
	{
		private readonly SveDiplomacyDelegate _testee = new();

		[Fact]
		public void EmbassiesAreWrittenAsFlag0x40InPlayerRow()
		{
			List<byte>[] embassies = [[], [3], [1, 5]];

			ushort[] block = _testee.ToSve(embassies);

			Assert.Equal(64, block.Length);
			Assert.Equal(0x40, block[(1 * 8) + 3]);
			Assert.Equal(0x40, block[(2 * 8) + 1]);
			Assert.Equal(0x40, block[(2 * 8) + 5]);
			Assert.Equal(3, System.Array.FindAll(block, v => v != 0).Length);
		}

		[Fact]
		public void EmbassiesAreReadFromFlag0x40AndIgnoreOtherFlags()
		{
			ushort[] block = new ushort[64];
			block[(4 * 8) + 2] = 0x41;
			block[(4 * 8) + 6] = 0x03;
			block[(5 * 8) + 1] = 0x40;

			ushort[] row = _testee.RowOf(block, 4);

			Assert.Equal(0x41, row[2]);
			Assert.Equal(0x03, row[6]);
			Assert.Equal([(byte)2], _testee.EmbassiesOf(row));
		}

		[Fact]
		public void EmbassiesRoundtrip()
		{
			List<byte>[] embassies = [[], [2, 7], [], [1]];

			ushort[] block = _testee.ToSve(embassies);

			Assert.Equal([(byte)2, 7], _testee.EmbassiesOf(_testee.RowOf(block, 1)));
			Assert.Equal([(byte)1], _testee.EmbassiesOf(_testee.RowOf(block, 3)));
			Assert.Empty(_testee.EmbassiesOf(_testee.RowOf(block, 0)));
		}
	}
}
