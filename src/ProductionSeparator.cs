// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
