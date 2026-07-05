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

		/// <summary>
		/// Gets the value of the attribute. Throws an exception if the value is invalid.
		/// </summary>
		/// <typeparam name="T">The expected type of the attribute value.</typeparam>
		/// <returns>The value of the attribute.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the attribute value is invalid.</exception>
		internal T GetRequiredValue<T>() where T : notnull
		{
			if (!Valid)
			{
				Debug.WriteLine($"Invalid attribute value: {_value} (type: {_value.GetType()})");
				throw new InvalidOperationException($"Invalid attribute value: {_value}");
			}

			return (T)_value;
		}

		/// <summary>
		/// Indicates whether the attribute value is valid.
		/// </summary>
		public bool Valid { get; }

		internal BaseAttribute(Type type, object value, Func<object, bool>? checkValue = null)
		{
			_value = value;
			Valid = (value.GetType() == type) && (checkValue == null || checkValue(value));
		}
	}
}