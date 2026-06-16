using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.UnitTests
{
	/// <summary>
	/// A helper class to supply mocked values in unit tests.
	/// Each call to Next() returns the next value in the supplied list.
	/// If the end of the list is reached, the last value is returned for all subsequent calls.
	/// The list of values can be reset using Reset().
	/// If no values are supplied, an InvalidOperationException is thrown when Next() is called.
	/// If default constructor is used, a single default value of type T is supplied.
	/// </summary>
	/// <typeparam name="T">The type of values to supply.</typeparam>
	/// <param name="values">The initial list of values to supply. If null, an ArgumentNullException is thrown.</param>
	public class SupplyMockedValues<T>(IEnumerable<T?> values)
	{
		private readonly List<T?> _values = [.. values ?? throw new ArgumentNullException(nameof(values))];
		private int _index;

		/// <summary>
		/// Initializes a new instance of the SupplyMockedValues class with a single default value of type T.
		/// </summary>
		public SupplyMockedValues() : this([default])
		{
		}

		/// <summary>
		/// Returns the next value in the list of supplied values. 
		/// If the end of the list is reached, the last value is returned for all subsequent calls.
		/// </summary>
		/// <returns>The next value in the list of supplied values.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no values have been supplied to the instance.</exception>
		public T? Next()
		{
			if (_values.Count == 0)
			{
				throw new InvalidOperationException("No values have been supplied.");
			}

			if (_index < _values.Count)
				return _values[_index++];

			return _values.Last();
		}

		/// <summary>
		/// Resets the index to the beginning of the list of supplied values.
		/// </summary>
		public void Reset()
		{
			_index = 0;
		}

		/// <summary>
		/// Resets the list of supplied values and the index.
		/// You should call this method if you want to change the list of values to supply or reset the index to the beginning of the list.
		/// If you only want to reset the index to the beginning of the list, you can call the parameterless Reset() method instead.
		/// Make sure to supply a list with at least one value, otherwise an InvalidOperationException will be thrown when Next() is called.
		/// </summary>
		/// <param name="values">The new list of values to supply. If null, an ArgumentNullException is thrown.</param>
		public void Reset(IEnumerable<T?> values)
		{
			_values.Clear();
			_values.AddRange(values ?? throw new ArgumentNullException(nameof(values)));
			_index = 0;
		}
	}
}