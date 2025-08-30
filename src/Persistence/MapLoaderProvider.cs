using System;
using System.Collections.Generic;
using CivOne.Persistence.Impl;
using CivOne.Persistence.Original.Impl;
using CivOne.Persistence.Original.Load;

namespace CivOne.Persistence
{
	public class MapLoaderProvider : IMapLoaderProvider
	{
		private readonly Dictionary<Type, IMapLoader> _loaders = new();
		private readonly IMapFactory _mapFactory = new MapFactoryImpl();

		public MapLoaderProvider()
		{
			// Manuelles Registrieren (oder über DI-Container)
			_loaders[typeof(OriginalMapLoaderImpl)] = new OriginalMapLoaderImpl(_mapFactory);
			// _loaders[typeof(JsonMapLoaderImpl)] = new JsonMapLoaderImpl();
		}

		public IMapLoader GetLoader<T>() where T : IMapLoader
		{
			if (_loaders.TryGetValue(typeof(T), out var loader))
			{
				return loader;
			}
			throw new NotSupportedException($"Loader {typeof(T).Name} is not registered.");
		}
	}
}