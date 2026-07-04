namespace CivOne.Services.Maps
{
	internal static class MapBitmapScalerFactory
	{
		private static IMapBitmapScaler? _cached;
		private static Settings.MapBitmapScalerType? _cachedMode;

		public static IMapBitmapScaler Create()
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