namespace CivOne.Persistence.Model
{
	using System;
	using System.Linq;
	using CivOne.Persistence.Mapper;

    public class ProductionDtoMapper(IReflect reflect) : DtoMapper<ProductionDto, IProduction>
	{
		public IProduction FromDto(ProductionDto dto)
		{
			return 
				reflect.GetProduction()
				.FirstOrDefault(p => p.ProductionId == dto.ProductionId)
				?? throw new InvalidOperationException(
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
