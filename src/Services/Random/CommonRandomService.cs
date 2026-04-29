using System;

namespace CivOne.Services.Random
{
	internal sealed class CommonRandomService : IRandomService
	{
		private readonly Func<CivOne.Random> _randomAccessor;

		public CommonRandomService(Func<CivOne.Random> randomAccessor)
		{
			_randomAccessor = randomAccessor ?? throw new ArgumentNullException(nameof(randomAccessor));
		}

		public int Next(int max) => _randomAccessor().Next(max);

		public int Next(int min, int max) => _randomAccessor().Next(min, max);

		public bool Hit(int percent) => _randomAccessor().Hit(percent);
	}
}
