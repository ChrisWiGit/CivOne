using System;
using System.Collections.Generic;
using CivOne.Persistence.Impl;

namespace CivOne.Persistence
{
	public class GameLoaderProvider : IGameLoaderProvider
	{
		private readonly Dictionary<Type, IGameLoader> _loaders = new();

		public GameLoaderProvider()
		{
			// Manuelles Registrieren (oder über DI-Container)
			_loaders[typeof(OriginalGameLoaderImpl)] = new OriginalGameLoaderImpl();
			// _loaders[typeof(JsonGameLoaderImpl)] = new JsonGameLoaderImpl();
		}

		public IGameLoader GetLoader<T>() where T : IGameLoader
		{
			if (_loaders.TryGetValue(typeof(T), out var loader))
			{
				return loader;
			}
			throw new NotSupportedException($"Loader {typeof(T).Name} is not registered.");
		}
	}
}