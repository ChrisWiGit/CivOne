namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Persistence.Mapper;
	using CivOne.Services.GlobalWarming;
	using Xunit;

	public class GlobalWarmingDtoMapperTest
	{
		private readonly GlobalWarmingDtoMapper _testee;
		private readonly GlobalWarmingDto _dto;

		public GlobalWarmingDtoMapperTest()
		{
			var sanitizer = new ValueSanitizer(new NoOpLogger());
			_testee = new GlobalWarmingDtoMapper(sanitizer);

			_dto = new GlobalWarmingDto
			{
				GlobalWarmingCount = 3,
				PollutedSquaresCount = 7,
				WarmingIndicator = WarmingIndicator.Yellow
			};
		}

		[Fact]
		public void TestGlobalWarmingDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<GlobalWarmingDto>();
			var expectedProperties = GetGlobalWarmingDtoRoundTripAssertionMap(_dto, _dto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestGlobalWarmingDtoMapper_RoundTrip()
		{
			// Arrange
			var expected = _dto;

			// Act
			var roundTripDto = _testee.ToDto(_testee.FromDto(expected));

			// Assert
			Assert.NotNull(roundTripDto);

			var assertions = GetGlobalWarmingDtoRoundTripAssertionMap(expected, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetGlobalWarmingDtoRoundTripAssertionMap(GlobalWarmingDto expected, GlobalWarmingDto actual)
			=> new()
			{
				[nameof(GlobalWarmingDto.GlobalWarmingCount)] = () => Assert.Equal(expected.GlobalWarmingCount, actual.GlobalWarmingCount),
				[nameof(GlobalWarmingDto.PollutedSquaresCount)] = () => Assert.Equal(expected.PollutedSquaresCount, actual.PollutedSquaresCount),
				[nameof(GlobalWarmingDto.WarmingIndicator)] = () => Assert.Equal(expected.WarmingIndicator, actual.WarmingIndicator)
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => [.. typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)];
	}
}
