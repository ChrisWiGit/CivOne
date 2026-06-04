// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;

namespace CivOne
{
	public abstract class BaseAttribute : Attribute
	{
		private readonly object _value;

		internal T? GetValue<T>() {
			if (!Valid)
			{
				Debug.WriteLine($"Invalid attribute value: {_value} (type: {_value.GetType()})");
			}
			return Valid ? (T)_value : default;
		}

		public bool Valid { get; }

		internal BaseAttribute(Type type, object value, Func<object, bool>? checkValue = null)
		{
			_value = value;
			Valid = (value.GetType() == type) && (checkValue == null || checkValue(value));
		}
	}
}