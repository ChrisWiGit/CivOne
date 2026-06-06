// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Units
{
	/// <summary>
	/// Modify the unit name.
	/// </summary>
	/// <param name="value">The new name for the unit. Must be between 1 and 12 characters long.</param>
	public class Name(string value) : BaseAttribute(typeof(string), value, InRange)
	{
		private static bool InRange(object value) => value is string str && str.Length > 0 && str.Length <= 12;

		public string Value => GetRequiredValue<string>();
	}
}