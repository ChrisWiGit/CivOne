using CivOne.Enums;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class SpaceShipGridMap2dTest
	{
		[Fact]
		public void ConstructorFromRows_PreservesRowColumnOrientation()
		{
			string[] rows =
			[
				"EEEKKEEEEFPE",
				"EOOKKOOENHHE",
				"EOOEEOOEVFPE",
				"HHHHHHHHNFPE",
				"BBLLBBLLNHHE",
				"BBLLBBLLVFPE",
				"LLBBLLBBVFPE",
				"LLBBLLBBNHHE",
				"HHHHHHHHNFPE",
				"EOOEEOOEVFPE",
				"EOOEEOOENHHE",
				"EEEEEEEEEFPE"
			];

			var actual = new SpaceShipGridMap2d(rows);

			Assert.Equal(rows, actual.Rows);
			Assert.Equal(SpaceShipComponentType.CommandModule, actual[3, 0]);
			Assert.Equal(SpaceShipComponentType.StructureHorizontal, actual[0, 3]);
		}
	}
}
