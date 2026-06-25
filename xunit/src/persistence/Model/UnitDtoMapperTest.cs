using System;
using System.Collections.Generic;
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
		private readonly UnitDtoMapper _testee;
		private readonly UnitFactory _unitFactory;
		private readonly UnitDto _originalDto;

		public UnitDtoMapperTest()
		{
			_unitFactory = new UnitFactory();
			_testee = new UnitDtoMapper(_unitFactory, new ValueSanitizer(new NoOpLogger()));

			// Code how to get all Unit class names for DocAttribute. 
			// UnitDto.AllUnitsClassNames = [.. typeof(IUnit).Assembly.GetTypes()
			// 	.Where(t => typeof(IUnit).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
			// 	.Select(t => t.Name)];
			UnitDto.AllUnitsClassNames = ["MockedIUnit"];

			_originalDto = new UnitDto
			{
				ClassName = "MockedIUnit",
				Location = new MapLocation(10, 20),
				Goto = new MapLocation(5, 8),
				HomeCityGuid = null,
				Busy = true,
				Veteran = true,
				Sentry = true,
				FortifyActive = true,
				Fortify = true,
				FuelOrProgress = 7,
				Fuel = 3,
				WorkProgress = 2,
				Order = Order.Fortify,
				MovesSkip = 1,
				MovesLeft = 2,
				PartMoves = 1,
				PlayerId = ExpectedPlayerId,
			};
		}

		[Fact]
		public void TestUnitDtoMapperContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<UnitDto>();
			var expectedProperties = GetUnitDtoRoundTripAssertionMap(_originalDto, _originalDto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
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
			Assert.Equal("MockedIUnit", _unitFactory.LastClassName);
			Assert.Equal(ExpectedHomeCityGuid, _unitFactory.LastHomeCityGuid);
			Assert.Equal(ExpectedPlayerId, _unitFactory.LastPlayerId);
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

		[Fact]
		public void TestUnitDtoMapperRoundTrip()
		{
			var unit = _testee.FromDto(_originalDto);
			var roundTripDto = _testee.ToDto(unit);

			Assert.NotNull(roundTripDto);

			var assertions = GetUnitDtoRoundTripAssertionMap(_originalDto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		[Theory]
		[InlineData(false, true, true, false)]
		[InlineData(false, false, false, true)]
		public void FromDtoUsesRestorableStatusMapping(
			bool expectedSentry,
			bool expectedFortifyActive,
			bool expectedFortify,
			bool expectedVeteran)
		{
			var dto = new UnitDto
			{
				ClassName = "MockedIUnit",
				Location = new MapLocation(10, 20),
				Goto = new MapLocation(5, 8),
				Sentry = expectedSentry,
				FortifyActive = expectedFortifyActive,
				Fortify = expectedFortify,
				Veteran = expectedVeteran,
				PlayerId = ExpectedPlayerId,
			};

			var restored = _testee.FromDto(dto);

			Assert.Equal(expectedSentry, restored.Sentry);
			Assert.Equal(expectedFortifyActive, restored.FortifyActive);
			Assert.Equal(expectedFortify, restored.Fortify);
			Assert.Equal(expectedVeteran, restored.Veteran);
		}

		private static Dictionary<string, Action> GetUnitDtoRoundTripAssertionMap(UnitDto expected, UnitDto actual)
			=> new()
			{
				[nameof(UnitDto.ClassName)] = () => Assert.Equal(expected.ClassName, actual.ClassName),
				[nameof(UnitDto.Location)] = () => Assert.Equal(expected.Location, actual.Location),
				[nameof(UnitDto.Goto)] = () => Assert.Equal(expected.Goto, actual.Goto),
				[nameof(UnitDto.HomeCityGuid)] = () => Assert.Equal(expected.HomeCityGuid, actual.HomeCityGuid),
				[nameof(UnitDto.Busy)] = () => Assert.Equal(expected.Busy, actual.Busy),
				[nameof(UnitDto.Veteran)] = () => Assert.Equal(expected.Veteran, actual.Veteran),
				[nameof(UnitDto.Sentry)] = () => Assert.Equal(expected.Sentry, actual.Sentry),
				[nameof(UnitDto.FortifyActive)] = () => Assert.Equal(expected.FortifyActive, actual.FortifyActive),
				[nameof(UnitDto.Fortify)] = () => Assert.Equal(expected.Fortify, actual.Fortify),
				[nameof(UnitDto.FuelOrProgress)] = () => Assert.Equal(expected.FuelOrProgress, actual.FuelOrProgress),
				[nameof(UnitDto.Fuel)] = () => Assert.Equal(expected.Fuel, actual.Fuel),
				[nameof(UnitDto.WorkProgress)] = () => Assert.Equal(expected.WorkProgress, actual.WorkProgress),
				[nameof(UnitDto.Order)] = () => Assert.Equal(expected.Order, actual.Order),
				[nameof(UnitDto.MovesSkip)] = () => Assert.Equal(expected.MovesSkip, actual.MovesSkip),
				[nameof(UnitDto.MovesLeft)] = () => Assert.Equal(expected.MovesLeft, actual.MovesLeft),
				[nameof(UnitDto.PartMoves)] = () => Assert.Equal(expected.PartMoves, actual.PartMoves),
				[nameof(UnitDto.PlayerId)] = () => Assert.Equal(expected.PlayerId, actual.PlayerId)
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite && !(p.GetMethod?.IsStatic ?? false))
			.Select(p => p.Name)
			.ToHashSet();


		public class UnitFactory : IUnitFactory
		{
			public string LastClassName { get; private set; } = string.Empty;
			public byte LastPlayerId { get; private set; }
			public Guid? LastHomeCityGuid { get; private set; }

			public IUnitRestorable Create(string className, byte player, Guid? homeCityGuid)
			{
				LastClassName = className;
				LastPlayerId = player;
				LastHomeCityGuid = homeCityGuid;
				// * var result = new MockedIUnit
				// * {
				// * 	Owner = player //just for testing, do not do this in real code because index of player it game list may not be the same as player id in save file.
				// * };
				// Just for fun, let's use reflection to create the instance instead of hardcoding it. This way we can test that the className is used correctly.
				var result = Activator.CreateInstance(Type.GetType($"CivOne.UnitTests.{className}")!) as IUnitRestorable
					?? throw new InvalidOperationException($"Failed to create instance of class '{className}' for unit restoration. Ensure that the class name is correct and that it implements IUnitRestorable.");
				result.Owner = player;
				// production code: add unit to Player

				return result;
			}
		}
	}
}