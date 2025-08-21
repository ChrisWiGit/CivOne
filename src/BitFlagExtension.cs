using System;
namespace CivOne
{
	/// <summary>
	/// Provides generic bit flag operations for working with enums that represent bit positions.
	/// </summary>
	/// <remarks>
	/// This utility allows you to treat any enum values as bit positions (0 = first bit, 1 = second bit, etc.)
	/// and store multiple states inside a numeric flag type (e.g. <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>).
	/// 
	/// Example:
	/// <code>
	/// public enum CityStatus
	/// {
	///     RIOT = 0,
	///     COASTAL = 1,
	///     AUTO_BUILD = 4
	/// }
	///
	/// ushort flags = 0;
	///
	/// // Set bits
	/// flags = BitFlagExtensions.SetFlag(flags, CityStatus.RIOT);
	/// flags = BitFlagExtensions.SetFlag(flags, CityStatus.AUTO_BUILD);
	///
	/// // Check bits
	/// bool riot = BitFlagExtensions.HasFlag(flags, CityStatus.RIOT);       // true
	/// bool coastal = BitFlagExtensions.HasFlag(flags, CityStatus.COASTAL); // false
	///
	/// // Clear bit
	/// flags = BitFlagExtensions.ClearFlag(flags, CityStatus.RIOT);
	///
	/// // Toggle bit
	/// flags = BitFlagExtensions.ToggleFlag(flags, CityStatus.AUTO_BUILD);
	/// </code>
	/// </remarks>
	public static class BitFlagExtensions
	{
		/// <summary>
		/// Checks whether a specific enum bit is set inside the given flag value.
		/// </summary>
		/// <typeparam name="TFlags">The numeric type used to store flags (e.g. <c>ushort</c>, <c>int</c>, <c>long</c>).</typeparam>
		/// <typeparam name="TEnum">The enum type representing the bit positions.</typeparam>
		/// <param name="flags">The current flag storage value.</param>
		/// <param name="value">The enum value representing the bit position to check.</param>
		/// <returns><c>true</c> if the bit is set; otherwise <c>false</c>.</returns>
		public static bool HasFlag<TFlags, TEnum>(TFlags flags, TEnum value)
			where TFlags : struct, IConvertible
			where TEnum : struct, Enum
		{
			long flagVal = Convert.ToInt64(flags);
			long mask = 1L << Convert.ToInt32(value);
			return (flagVal & mask) != 0;
		}

		/// <summary>
		/// Sets a specific enum bit inside the given flag value.
		/// </summary>
		/// <typeparam name="TFlags">The numeric type used to store flags (e.g. <c>ushort</c>, <c>int</c>, <c>long</c>).</typeparam>
		/// <typeparam name="TEnum">The enum type representing the bit positions.</typeparam>
		/// <param name="flags">The current flag storage value.</param>
		/// <param name="value">The enum value representing the bit position to set.</param>
		/// <returns>The updated flag storage value with the bit set.</returns>
		public static TFlags SetFlag<TFlags, TEnum>(TFlags flags, TEnum value)
			where TFlags : struct, IConvertible
			where TEnum : struct, Enum
		{
			long flagVal = Convert.ToInt64(flags);
			long mask = 1L << Convert.ToInt32(value);
			long result = flagVal | mask;
			return (TFlags)Convert.ChangeType(result, typeof(TFlags));
		}

		/// <summary>
		/// Clears a specific enum bit inside the given flag value.
		/// </summary>
		/// <typeparam name="TFlags">The numeric type used to store flags (e.g. <c>ushort</c>, <c>int</c>, <c>long</c>).</typeparam>
		/// <typeparam name="TEnum">The enum type representing the bit positions.</typeparam>
		/// <param name="flags">The current flag storage value.</param>
		/// <param name="value">The enum value representing the bit position to clear.</param>
		/// <returns>The updated flag storage value with the bit cleared.</returns>
		public static TFlags ClearFlag<TFlags, TEnum>(TFlags flags, TEnum value)
			where TFlags : struct, IConvertible
			where TEnum : struct, Enum
		{
			long flagVal = Convert.ToInt64(flags);
			long mask = 1L << Convert.ToInt32(value);
			long result = flagVal & ~mask;
			return (TFlags)Convert.ChangeType(result, typeof(TFlags));
		}

		/// <summary>
		/// Toggles (inverts) a specific enum bit inside the given flag value.
		/// </summary>
		/// <typeparam name="TFlags">The numeric type used to store flags (e.g. <c>ushort</c>, <c>int</c>, <c>long</c>).</typeparam>
		/// <typeparam name="TEnum">The enum type representing the bit positions.</typeparam>
		/// <param name="flags">The current flag storage value.</param>
		/// <param name="value">The enum value representing the bit position to toggle.</param>
		/// <returns>The updated flag storage value with the bit toggled.</returns>
		public static TFlags ToggleFlag<TFlags, TEnum>(TFlags flags, TEnum value)
			where TFlags : struct, IConvertible
			where TEnum : struct, Enum
		{
			long flagVal = Convert.ToInt64(flags);
			long mask = 1L << Convert.ToInt32(value);
			long result = flagVal ^ mask;
			return (TFlags)Convert.ChangeType(result, typeof(TFlags));
		}
	}
}