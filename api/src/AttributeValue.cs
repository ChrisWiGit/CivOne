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
	public class AttributeValue<T> where T : notnull
	{
		public bool HasValue { get; }
		public T? Value { get; }

		internal static AttributeValue<T> Set(BaseAttribute attribute) => new(attribute);

		private AttributeValue(BaseAttribute attribute)
		{
			if (attribute == null || !attribute.Valid)
			{
				HasValue = false;
				return;
			}

			HasValue = true;
			Value = attribute.GetRequiredValue<T>();
		}
	}
}