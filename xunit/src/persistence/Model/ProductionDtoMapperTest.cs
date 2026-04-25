using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class ProductionDtoMapperTest
	{
		private readonly IReflect _reflect = new MockedReflect();
		private readonly ProductionDtoMapper _testee;
		private readonly ProductionDto _originalDto;

		public ProductionDtoMapperTest()
		{
			_testee = new ProductionDtoMapper(_reflect);
			var production = _reflect.GetProduction().First(p => p.Price > 0);
			_originalDto = new ProductionDto
			{
				Price = production.Price,
				BuyPrice = (uint)production.BuyPrice,
				ProductionId = production.ProductionId
			};
		}

		[Fact]
		public void TestProductionDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<ProductionDto>();
			var expectedProperties = GetProductionDtoRoundTripAssertionMap(_originalDto, _originalDto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void ToDto_MapsAllFieldsCorrectly()
		{
			IProduction production = _reflect.GetProduction().First();

			ProductionDto dto = _testee.ToDto(production);

			Assert.Equal((uint)production.Price, dto.Price);
			Assert.Equal((uint)production.BuyPrice, dto.BuyPrice);
			Assert.Equal((uint)production.ProductionId, dto.ProductionId);
		}

		[Fact]
		public void FromDto_WithValidProductionId_ReturnsMatchingProduction()
		{
			// 0 is okay, but we want to test another one.
			IProduction expected = _reflect.GetProduction().First(p => p.Price > 0);
			var dto = new ProductionDto { ProductionId = expected.ProductionId };

			IProduction actual = _testee.FromDto(dto);

			Assert.Equal(expected.ProductionId, actual.ProductionId);
		}

		[Fact]
		public void FromDto_WithInvalidProductionId_ThrowsException()
		{
			var dto = new ProductionDto { ProductionId = uint.MaxValue };

			Assert.Throws<Exception>(() => _testee.FromDto(dto));
		}

		[Fact]
		public void TestProductionDtoMapper_RoundTrip()
		{
			var restoredProduction = _testee.FromDto(_originalDto);
			var roundTripDto = _testee.ToDto(restoredProduction);

			Assert.NotNull(roundTripDto);

			var assertions = GetProductionDtoRoundTripAssertionMap(_originalDto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetProductionDtoRoundTripAssertionMap(ProductionDto expected, ProductionDto actual)
			=> new()
			{
				[nameof(ProductionDto.Price)] = () => Assert.Equal(expected.Price, actual.Price),
				[nameof(ProductionDto.BuyPrice)] = () => Assert.Equal(expected.BuyPrice, actual.BuyPrice),
				[nameof(ProductionDto.ProductionId)] = () => Assert.Equal(expected.ProductionId, actual.ProductionId)
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)
			.ToHashSet();
	}
}
