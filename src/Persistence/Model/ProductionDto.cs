namespace CivOne.Persistence.Model
{
    using CityId = System.UInt32;
    using PlayerId = System.Byte;

    public class ProductionDto
	{
		public uint Price { get; set; }
		public uint BuyPrice { get; set; }

		public uint ProductionId { get; set; }
	}
}
