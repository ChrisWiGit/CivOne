namespace CivOne.Services.Random
{
	/// <summary>
	/// Use this factory to create an instance of <see cref="IRandomService"/>. 
	/// The factory will cache the created instance and return the same instance on subsequent calls to <see cref="Create"/>. 
	/// Use <see cref="Reset"/> to reset the cached instance with a new seed or to clear the cache.
	/// </summary>
	internal static class RandomServiceFactory
	{
		private static IRandomService _cached;

		public static IRandomService Create()
		{
			if (_cached != null)
			{
				return _cached;
			}

			if (Common.Random == null)
			{
				Common.SetRandomSeed();
			}

			_cached = new CommonRandomService(() => Common.Random);
			return _cached;
		}

		public static IRandomService Reset(ushort seed)
		{
			Common.SetRandomSeed(seed);
			_cached = new CommonRandomService(() => Common.Random);
			return _cached;
		}

		public static IRandomService Reset()
		{
			Common.SetRandomSeed();
			_cached = new CommonRandomService(() => Common.Random);
			return _cached;
		}

		internal static IRandomService Override(IRandomService randomService)
		{
			_cached = randomService;
			return _cached;
		}
	}
}
