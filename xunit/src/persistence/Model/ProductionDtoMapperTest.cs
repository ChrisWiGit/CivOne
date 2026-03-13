using System;
using System.Linq;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class ProductionDtoMapperTest : TestsBase2
	{
		private readonly IReflect _reflect = new GameReflect();
		private readonly ProductionDtoMapper _testee;

		public ProductionDtoMapperTest()
		{
			_testee = new ProductionDtoMapper(_reflect);
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
		public void RoundTrip_ToDtoThenFromDto_PreservesProductionId()
		{
			foreach (IProduction production in _reflect.GetProduction())
			{
				ProductionDto dto = _testee.ToDto(production);
				IProduction restored = _testee.FromDto(dto);

				Assert.Equal(production.ProductionId, restored.ProductionId);
			}
		}
	}
}
