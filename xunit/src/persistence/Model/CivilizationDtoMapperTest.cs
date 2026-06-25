namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.UnitTests;
	using Xunit;

	public class CivilizationDtoMapperTest
	{
		private readonly CivilizationDtoMapper _testee;
		private readonly CivilizationDto _originalDto;

		public CivilizationDtoMapperTest()
		{
			var civilizations = MockedICivilization.Mock(3);
			CivilizationDto.AllLeaderClassNames = [.. civilizations.Select(c => c.Leader.GetType().Name).Distinct()];

			_testee = new CivilizationDtoMapper(civilizations);
			_originalDto = new CivilizationDto
			{
				LeaderClassName = civilizations[0].Leader.GetType().Name
			};
		}

		[Fact]
		public void TestCivilizationDtoMapperContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<CivilizationDto>();
			var expectedProperties = GetCivilizationDtoRoundTripAssertionMap(_originalDto, _originalDto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestCivilizationDtoMapperRoundTrip()
		{
			var civilization = _testee.FromDto(_originalDto);
			var roundTripDto = _testee.ToDto(civilization);

			Assert.NotNull(roundTripDto);

			var assertions = GetCivilizationDtoRoundTripAssertionMap(_originalDto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetCivilizationDtoRoundTripAssertionMap(CivilizationDto expected, CivilizationDto actual)
			=> new()
			{
				[nameof(CivilizationDto.LeaderClassName)] = () => Assert.Equal(expected.LeaderClassName, actual.LeaderClassName)
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite && !(p.GetMethod?.IsStatic ?? false))
			.Select(p => p.Name)
			.ToHashSet();
	}
}
