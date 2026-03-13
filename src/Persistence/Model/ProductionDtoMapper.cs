namespace CivOne.Persistence.Model
{
	using System.Linq;
	using CityId = System.UInt32;
    using PlayerId = System.Byte;

    public class ProductionDtoMapper(IReflect _reflect) : DtoMapper<ProductionDto, IProduction>
	{
		public IProduction FromDto(ProductionDto dto)
		{
			return 
				_reflect.GetProduction()
				.FirstOrDefault(p => p.ProductionId == dto.ProductionId)
				?? throw new System.Exception(
					$"Production with id {dto.ProductionId} not found. DTO details: Price={dto.Price}, BuyPrice={dto.BuyPrice}");
		}

		public ProductionDto ToDto(IProduction domain)
		{
			return new ProductionDto
			{
				Price = domain.Price,
				BuyPrice = (uint)domain.BuyPrice,
				ProductionId = domain.ProductionId
			};
		}
	}
}
