using System;
using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Provides commands for mutating Hall of Fame storage, e.g. clearing entries.
	/// </summary>
	/// <remarks>
	/// Persists changes via <see cref="IHallOfFameFileRepository"/> and composes entries with the provided composer.
	/// </remarks>
	internal sealed class HallOfFameCommandService(
		string storageDirectory,
		IHallOfFamePersistService persistService,
		IHallOfFameFileRepository fileRepository,
		IHallOfFameEntryComposerService entryComposerService,
		Action<string> log = null) : IHallOfFameCommandService
	{
		private readonly string _storageDirectory = storageDirectory;
		private readonly IHallOfFamePersistService _persistService = persistService;
		private readonly IHallOfFameFileRepository _fileRepository = fileRepository;
		private readonly IHallOfFameEntryComposerService _entryComposerService = entryComposerService;
		private readonly Action<string> _log = log;

		public IReadOnlyList<HallOfFameEntry> Clear()
		{
			IReadOnlyList<HallOfFameEntry> clearedEntries = BuildClearedEntries();

			if (!_fileRepository.TrySave(_storageDirectory, clearedEntries, out string saveError))
			{
				_log?.Invoke($"Could not clear Hall of Fame entries: {saveError}");
				return _persistService.ViewEntries(_storageDirectory, _log);
			}

			return _persistService.ViewEntries(_storageDirectory, _log);
		}

		private IReadOnlyList<HallOfFameEntry> BuildClearedEntries()
		{
			try
			{
				HallOfFameEntry currentEntry = _entryComposerService.ComposeForHuman();
				return [currentEntry];
			}
			catch (InvalidOperationException ex)
			{
				_log?.Invoke($"Could not compose current human Hall of Fame entry while clearing. Falling back to empty list. Reason: {ex.Message}");
				return [];
			}
		}
	}
}
