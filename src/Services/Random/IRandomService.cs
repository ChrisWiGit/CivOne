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
	}
}
