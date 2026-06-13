namespace CivOne.Services.Random
{
	public interface IRandomService
	{
		int NextInt(int maxExclusive);
		int NextInt(int min, int maxExclusive);
		byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);
		byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);

		ushort NextUShort(ushort maxExclusive) => (ushort)NextInt(maxExclusive);
		ushort NextUShort(ushort min, ushort maxExclusive) => (ushort)NextInt(min, maxExclusive);
		
		/// <summary>
		/// Determines if a random event occurs based on a percentage chance.
		/// </summary>
		/// <param name="percent">The percentage chance (0-100) for the event to occur.</param>
		/// <returns>True if the random event occurs, false otherwise.</returns>
		/// <remarks>
		/// This method generates a random number between 0 and 99 (inclusive) and compares it
		/// against the provided percentage. For example, a 30% chance means the method returns
		/// <c>true</c> approximately 30% of the time when called repeatedly.
		/// </remarks>
		bool Hit(int percent);


		/// <summary>
		/// Determines if a random event occurs based on a fractional chance defined by a numerator and denominator.
		/// For example, a 1 in 4 chance would be represented as numerator=1 and denominator=4. The method returns true if the random event occurs, and false otherwise.
		/// The method generates a random number between 0 (inclusive) and the denominator (exclusive) and checks if it is less than the numerator. 
		/// If the numerator is greater than or equal to the denominator, the method will always return true. 
		/// If the numerator is less than or equal to zero, the method will always return false.
		/// Use this method when you want to represent probabilities that are not easily expressed as a percentage, such as a 1 in 3 chance (numerator=1, denominator=3) or a 2 in 5 chance (numerator=2, denominator=5).
		/// </summary>
		/// <param name="numerator">The numerator representing the number of successful outcomes. Must be non-negative.</param>
		/// <param name="denominator">The denominator representing the total number of possible outcomes. Must be positive.</param>
		/// <returns>True if the random event occurs, false otherwise.</returns>
		bool Hit(int numerator, int denominator);
	}
}
