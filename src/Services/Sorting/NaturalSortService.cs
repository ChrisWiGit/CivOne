using System;

namespace CivOne.Services.Sorting
{
	/// <summary>
	/// Default <see cref="INaturalSortService"/> implementation.
	/// </summary>
	/// <remarks>
	/// Walks both strings run by run, comparing digit runs by their numeric value
	/// (ignoring leading zeros) and non-digit runs case-insensitively.
	/// When the walk finds no difference (e.g. "map007" and "map7" share the same numeric
	/// value), the shorter raw string sorts first.
	/// </remarks>
	internal class NaturalSortService : INaturalSortService
	{
		/// <inheritdoc/>
		public int Compare(string? x, string? y)
		{
			if (ReferenceEquals(x, y))
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			int runCompare = CompareRuns(x, y);
			return runCompare != 0 ? runCompare : x.Length - y.Length;
		}

		private static int CompareRuns(string x, string y)
		{
			int i = 0;
			int j = 0;
			while (i < x.Length && j < y.Length)
			{
				if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
				{
					int digitCompare = CompareDigitRuns(x, ref i, y, ref j);
					if (digitCompare != 0)
					{
						return digitCompare;
					}

					continue;
				}

				int charCompare = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
				if (charCompare != 0)
				{
					return charCompare;
				}

				i++;
				j++;
			}

			return (x.Length - i) - (y.Length - j);
		}

		/// <summary>
		/// Compares the digit runs starting at <paramref name="i"/> and <paramref name="j"/> by
		/// numeric value, then advances both indices past their respective runs.
		/// </summary>
		private static int CompareDigitRuns(string x, ref int i, string y, ref int j)
		{
			int startI = i;
			while (i < x.Length && char.IsDigit(x[i]))
			{
				i++;
			}

			int startJ = j;
			while (j < y.Length && char.IsDigit(y[j]))
			{
				j++;
			}

			ReadOnlySpan<char> digitsX = x.AsSpan(startI, i - startI).TrimStart('0');
			ReadOnlySpan<char> digitsY = y.AsSpan(startJ, j - startJ).TrimStart('0');

			if (digitsX.Length != digitsY.Length)
			{
				return digitsX.Length - digitsY.Length;
			}

			return digitsX.CompareTo(digitsY, StringComparison.Ordinal);
		}
	}
}
