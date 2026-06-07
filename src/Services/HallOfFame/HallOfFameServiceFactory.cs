using System;

namespace CivOne.Services.HallOfFame
{
		/// <summary>
		/// Factory for creating Hall of Fame related services and providing a configurable repository.
		/// </summary>
		internal static class HallOfFameServiceFactory
		{
		private static readonly object _sync = new();
		private static IHallOfFameFileRepository _repository = new HallOfFameFileRepositoryImpl(new AtomicFileReplacementService());

		public static IHallOfFamePersistService CreatePersistService()
		{
			lock (_sync)
			{
				return new HallOfFamePersistService(_repository);
			}
		}

		public static IHallOfFameCommandService CreateCommandService(
			string storageDirectory,
			IHallOfFamePersistService persistService,
			IHallOfFameEntryComposerService entryComposerService,
			Action<string>? log = null)
		{
			ArgumentNullException.ThrowIfNull(persistService);
			ArgumentNullException.ThrowIfNull(entryComposerService);

			lock (_sync)
			{
				return new HallOfFameCommandService(storageDirectory, persistService, _repository, entryComposerService, log);
			}
		}

		public static void ConfigureRepository(IHallOfFameFileRepository repository)
		{
			ArgumentNullException.ThrowIfNull(repository);

			lock (_sync)
			{
				_repository = repository;
			}
		}
	}
}
