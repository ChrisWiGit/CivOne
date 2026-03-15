using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Units;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class UnitDtoMapperTest
	{
		public static Guid ExpectedHomeCityGuid { get; set; } = Guid.NewGuid();

		public static byte ExpectedPlayerId { get; set; } = 123;
		private UnitDtoMapper _testee;
		public UnitDtoMapperTest()
		{
			_testee = new UnitDtoMapper(new UnitFactory());
		}

		[Fact]
		public void TestUnitToDto()
		{
			var unit = new MockedIUnit
			{
				Owner = ExpectedPlayerId,
			};
			var dto = _testee.ToDto(unit);
			Assert.NotNull(dto);
			Assert.Equal("MockedIUnit", dto.ClassName);

			Assert.Null(dto.HomeCityGuid);
			dto.HomeCityGuid = ExpectedHomeCityGuid;

			var restored = _testee.FromDto(dto);
			Assert.NotNull(restored);
			Assert.Equal(ExpectedPlayerId, restored.Owner);
			Assert.Equal(unit.Type, restored.Type);
			Assert.Equal(unit.GetType().Name, dto.ClassName);
			Assert.Equal(unit.Busy, restored.Busy);
			Assert.Equal(unit.HasAction, restored.HasAction);
			Assert.Equal(unit.HasMovesLeft, restored.HasMovesLeft);
			Assert.Equal(unit.Veteran, restored.Veteran);
			Assert.Equal(unit.Sentry, restored.Sentry);
			Assert.Equal(unit.FortifyActive, restored.FortifyActive);
			Assert.Equal(unit.Fortify, restored.Fortify);
			Assert.Equal(unit.FuelOrProgress, restored.FuelOrProgress);
			Assert.Equal(unit.Fuel, restored.Fuel);
			Assert.Equal(unit.WorkProgress, restored.WorkProgress);
			Assert.Equal(unit.order, restored.order);
			Assert.Equal(unit.MovesSkip, restored.MovesSkip);
			Assert.Equal(unit.MovesLeft, restored.MovesLeft);
			Assert.Equal(unit.PartMoves, restored.PartMoves);
		}


		public class UnitFactory : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
			{
				Assert.Equal("MockedIUnit", className);
				Assert.NotNull(HomeCityGuid);
				Assert.Equal(UnitDtoMapperTest.ExpectedHomeCityGuid, HomeCityGuid.Value);
				Assert.Equal(UnitDtoMapperTest.ExpectedPlayerId, player);
				// * var result = new MockedIUnit
				// * {
				// * 	Owner = player //just for testing, do not do this in real code because index of player it game list may not be the same as player id in save file.
				// * };
				// Just for fun, let's use reflection to create the instance instead of hardcoding it. This way we can test that the className is used correctly.
				var result = (IUnitRestorable)Activator.CreateInstance(Type.GetType($"CivOne.UnitTests.{className}"));
				result.Owner = player;
				// production code: add unit to Player

				return result;
			}
		}
	}
}