using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Provides persistence-level operations for Hall of Fame entries (load, add, normalize).
	/// </summary>
	internal sealed class HallOfFamePersistService(IHallOfFameFileRepository repository) : IHallOfFamePersistService
	{
		private const int MaxEntries = 5;
		private readonly IHallOfFameFileRepository _repository = repository;

		public IReadOnlyList<HallOfFameEntry> ViewEntries(string storageDirectory, Action<string>? log = null)
		{
			if (_repository.TryLoad(storageDirectory, out IReadOnlyList<HallOfFameEntry> entries, out string? error))
			{
				return Normalize(entries);
			}

			if (IsMissingFileError(error))
			{
				log?.Invoke($"Hall of Fame file missing. Showing placeholders. {error}");
				return [];
			}

			log?.Invoke($"Could not load Hall of Fame entries: {error}");
			return [];
		}

		public IReadOnlyList<HallOfFameEntry> AddEntry(string storageDirectory, HallOfFameEntry? entry, Action<string>? log = null)
		{
			ArgumentNullException.ThrowIfNull(entry);

			IReadOnlyList<HallOfFameEntry> loadedEntries = LoadForAdd(storageDirectory, log);
			List<HallOfFameEntry> mergedEntries = [entry, .. loadedEntries];
			IReadOnlyList<HallOfFameEntry> normalizedEntries = Normalize(mergedEntries);

			if (!_repository.TrySave(storageDirectory, normalizedEntries, out string? saveError))
			{
				log?.Invoke($"Could not save Hall of Fame entries: {saveError}");
			}

			return normalizedEntries;
		}

		private IReadOnlyList<HallOfFameEntry> LoadForAdd(string storageDirectory, Action<string>? log)
		{
			if (_repository.TryLoad(storageDirectory, out IReadOnlyList<HallOfFameEntry> entries, out string? error))
			{
				return entries;
			}

			if (!IsMissingFileError(error))
			{
				log?.Invoke($"Could not load Hall of Fame entries before add: {error}");
			}

			return [];
		}

		private static IReadOnlyList<HallOfFameEntry> Normalize(IReadOnlyList<HallOfFameEntry> entries)
		{
			return [.. entries
				.Where(entry => entry != null)
				.OrderByDescending(entry => entry.Score)
				.ThenByDescending(entry => entry.CreatedAtUtc)
				.Take(MaxEntries)];
		}

		private static bool IsMissingFileError(string? error)
			=> !string.IsNullOrWhiteSpace(error)
			&& error.StartsWith("File not found:", StringComparison.OrdinalIgnoreCase);
	}
}
