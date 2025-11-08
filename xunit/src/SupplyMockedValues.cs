using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.UnitTests
{
	/**
	 * A helper class to supply mocked values in unit tests.
	 * Each call to Next() returns the next value in the supplied list.
	 * If the end of the list is reached, the last value is returned for all subsequent calls.
	 * The list of values can be reset using Reset().
	 * If no values are supplied, an InvalidOperationException is thrown when Next() is called.
	 * If default constructor is used, a single default value of type T is supplied.
	 */
	public class SupplyMockedValues<T>
	{
		private readonly List<T> _values;
		private int _index = 0;

		public SupplyMockedValues(IEnumerable<T> values)
		{
			_values = [.. values ?? throw new ArgumentNullException(nameof(values))];
		}

		public SupplyMockedValues() : this([default])
		{
		}

		public T Next()
		{
			if (_values.Count == 0)
			{
				throw new InvalidOperationException("No values have been supplied.");
			}

			if (_index < _values.Count)
				return _values[_index++];

			return _values.Last();
		}

		public void Reset()
		{
			_index = 0;
		}

		public void Reset(IEnumerable<T> values)
		{
			_values.Clear();
			_values.AddRange(values ?? throw new ArgumentNullException(nameof(values)));
			_index = 0;
		}
	}
}