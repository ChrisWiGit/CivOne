namespace CivOne
{
	/// <summary>
	/// Separator item used in production menus to separate categories (Units, Buildings, Wonders)
	/// </summary>
	internal class ProductionSeparator : IProduction
	{
		public string Text { get; }

		public byte Price => 0;

		public short BuyPrice => 0;

		public byte ProductionId => 0;

		public ProductionSeparator(string text)
		{
			Text = text;
		}
	}
}