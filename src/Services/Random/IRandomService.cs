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
		
		bool Hit(int percent);
	}
}
