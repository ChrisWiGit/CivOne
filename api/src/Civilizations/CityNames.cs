// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;

namespace CivOne.Civilizations
{
	/// <summary>
	/// Modify the city names.
	/// </summary>
	/// <param name="value">A new list of city names. Must be an array of 16 strings, each between 1 and 13 characters long.</param>
	public sealed class CityNames(params string[] value) : BaseAttribute(typeof(string[]), value, InRange)
	{
		private static bool InRange(object value) => (value is string[] arr) && arr.Length == 16 && arr.All(x => x != null && x.Length >= 1 && x.Length <= 13);

		public string[] Value => GetRequiredValue<string[]>();
	}
}