namespace CivOne.Services.Maps
{
	/// <summary>
	/// Provides a default <see cref="IMapBitmapScaler"/> implementation based on <see cref="Settings.BitmapScalerMode"/>.
	/// </summary>
	internal static class MapBitmapScalerFactory
	{
		private static IMapBitmapScaler? _cached;
		private static Settings.MapBitmapScalerType? _cachedMode;

		/// <summary>
		/// Gets the default <see cref="IMapBitmapScaler"/> implementation based on <see cref="Settings.BitmapScalerMode"/>.
		/// </summary>
		/// <returns>An <see cref="IMapBitmapScaler"/> instance.</returns>
		public static IMapBitmapScaler GetDefault()
		{
			Settings.MapBitmapScalerType mode = Settings.Instance.BitmapScalerMode;
			if (_cached != null && _cachedMode == mode)
			{
				return _cached;
			}

			_cached = mode switch
			{
				Settings.MapBitmapScalerType.NearestNeighbor => new NearestNeighborMapBitmapScaler(),
				_ => new PaletteAwareWeightedMapBitmapScaler()
			};
			_cachedMode = mode;
			return _cached;
		}

		internal static IMapBitmapScaler Override(IMapBitmapScaler scaler)
		{
			_cached = scaler;
			_cachedMode = null;
			return _cached;
		}
	}
}