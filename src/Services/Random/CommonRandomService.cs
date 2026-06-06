using System;

namespace CivOne.Services.Random
{
	internal sealed class CommonRandomService(Func<CivOne.Random> randomAccessor) : IRandomService
	{
		private readonly Func<CivOne.Random> _randomAccessor = randomAccessor ?? throw new ArgumentNullException(nameof(randomAccessor));

		public int NextInt(int max) => _randomAccessor().Next(max);

		public int NextInt(int min, int max) => _randomAccessor().Next(min, max);

		public bool Hit(int percent) => _randomAccessor().Hit(percent);

		public byte NextByte(byte min, byte maxExclusive) => (byte)_randomAccessor().Next(min, maxExclusive);

		public byte NextByte(byte maxExclusive) => (byte)_randomAccessor().Next(maxExclusive);
	}
}
