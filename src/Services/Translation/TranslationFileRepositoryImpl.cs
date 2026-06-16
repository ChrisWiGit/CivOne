using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CivOne.Services.Translation
{
	/// <summary>
	/// Default file-system based implementation of <see cref="ITranslationFileRepository"/>.
	/// Reads translation files from the runtime translation directory and validates file structure.
	/// </summary>
	public class TranslationFileRepository : ITranslationFileRepository
	{
		private const string EqualsPlaceholder = "[EQ]";
		private const string LanguageDisplayNameKey = "__LANGUAGE_DISPLAYNAME__";

		private static readonly HashSet<string> ExcludedFileNames = new(StringComparer.Ordinal)
		{
			"all.txt",
			"obsoletekeys.txt"
		};

		/// <inheritdoc/>
		public int SyncFiles(string sourceDirectory, string targetDirectory, Action<string>? log = null)
		{
			Directory.CreateDirectory(targetDirectory);

			IEnumerable<string> candidates = Directory.GetFiles(sourceDirectory, "*.txt", SearchOption.TopDirectoryOnly)
				.Where(path =>
				{
					string fileName = Path.GetFileName(path);

					// Only accept files that are already lowercase on disk so the runtime
					// translations folder stays consistent across Windows/Linux/macOS without
					// platform-specific handling. Files with any uppercase letter (e.g. authoring
					// artifacts from case-insensitive systems) are skipped.
					if (!string.Equals(fileName, fileName.ToLowerInvariant(), StringComparison.Ordinal))
					{
						log?.Invoke($"Skipping translation file with non-lowercase name: {fileName}");
						return false;
					}

					if (ExcludedFileNames.Contains(fileName))
					{
						log?.Invoke($"Skipping non-translation file: {fileName}");
						return false;
					}

					return true;
				});

			var groups = candidates.GroupBy(path => Path.GetFileName(path)).ToList();

			int copiedCount = 0;
			foreach (var group in groups)
			{
				string[] groupFiles = [.. group];
				if (groupFiles.Length > 1)
				{
					log?.Invoke($"Translation file conflict (name clash): '{group.Key}' — skipping all: {string.Join(", ", groupFiles.Select(Path.GetFileName))}");
					continue;
				}

				string targetPath = Path.Combine(targetDirectory, group.Key);
				File.Copy(groupFiles[0], targetPath, true);
				copiedCount++;
			}

			return copiedCount;
		}

		/// <inheritdoc/>
		public IReadOnlyList<TranslationLanguageInfo> GetAvailableLanguages(string storageDirectory, Action<string>? log = null)
		{
			if (string.IsNullOrWhiteSpace(storageDirectory))
			{
				return [];
			}

			string translationDirectory = GetTranslationDirectory(storageDirectory);
			if (!Directory.Exists(translationDirectory))
			{
				return [];
			}

			List<TranslationLanguageInfo> output = [];
			foreach (string filePath in Directory.EnumerateFiles(translationDirectory, "*", SearchOption.TopDirectoryOnly)
				.Where(path => string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(filePath);
				if (!TryGetPostfix(fileName, out string? postfix))
				{
					continue;
				}

				if (!TryLoadTranslations(filePath, out IReadOnlyDictionary<string, string>? translations, out string? errorMessage))
				{
					log?.Invoke($"Skipping translation file '{fileName}': {errorMessage}");
					continue;
				}

				string? displayName = TryGetDisplayName(postfix!, translations);
				output.Add(new TranslationLanguageInfo(postfix!, filePath, displayName));
			}

			return output;
		}

		/// <inheritdoc/>
		public bool TryLoadTranslations(string filePath, out IReadOnlyDictionary<string, string>? translations, [NotNullWhen(false)] out string? errorMessage)
		{
			translations = null;
			errorMessage = null;

			if (string.IsNullOrWhiteSpace(filePath))
			{
				errorMessage = "File path is empty.";
				return false;
			}

			if (!File.Exists(filePath))
			{
				errorMessage = $"File not found: {filePath}";
				return false;
			}

			Dictionary<string, string> output = new(StringComparer.OrdinalIgnoreCase);
			try
			{
				string[] lines = File.ReadAllLines(filePath);
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}

					if (line.StartsWith('#'))
					{
						continue;
					}

					int separatorIndex = line.IndexOf('=');
					if (separatorIndex < 0)
					{
						errorMessage = $"Malformed line {i + 1}: missing '=' separator.";
						return false;
					}

					string key = NormalizeKey(UnescapeControlCharacters(UnescapeEquals(line[..separatorIndex])));
					if (key.Length == 0)
					{
						errorMessage = $"Malformed line {i + 1}: empty key.";
						return false;
					}

					string value = UnescapeControlCharacters(UnescapeEquals(line[(separatorIndex + 1)..]));
					output[key] = value;
				}
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}

			translations = output;
			return true;
		}

		private static bool TryGetPostfix(string fileName, out string? postfix)
		{
			postfix = null;
			if (string.IsNullOrEmpty(fileName)
				|| !fileName.StartsWith("civ_", StringComparison.Ordinal)
				|| !fileName.EndsWith(".txt", StringComparison.Ordinal))
			{
				return false;
			}

			postfix = fileName[4..^4];
			return postfix.Length > 0;
		}

		private static string GetTranslationDirectory(string storageDirectory) => Path.Combine(storageDirectory, "translations");

		private static string? TryGetDisplayName(string postfix, IReadOnlyDictionary<string, string>? translations)
		{
			if (translations is null)
			{
				return null;
			}

			if (TryGetNonEmptyValue(translations, LanguageDisplayNameKey, out string? displayName))
			{
				return displayName;
			}

			string legacyKey = NormalizeKey(postfix);
			if (TryGetNonEmptyValue(translations, legacyKey, out displayName))
			{
				return displayName;
			}

			return null;
		}

		private static bool TryGetNonEmptyValue(IReadOnlyDictionary<string, string> translations, string key, out string? value)
		{
			value = null;
			if (!translations.TryGetValue(key, out string? rawValue))
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(rawValue))
			{
				return false;
			}

			value = rawValue.Trim();
			return true;
		}

		private static string NormalizeKey(string key) => key?.Trim().ToUpperInvariant() ?? string.Empty;

		private static string UnescapeEquals(string value) => value.Replace(EqualsPlaceholder, "=", StringComparison.Ordinal);

		private static string UnescapeControlCharacters(string value) => value
			.Replace("\\R\\N", "\r\n", StringComparison.Ordinal)
			.Replace("\\r\\n", "\r\n", StringComparison.Ordinal)
			.Replace("\\N", "\n", StringComparison.Ordinal)
			.Replace("\\n", "\n", StringComparison.Ordinal)
			.Replace("\\R", "\r", StringComparison.Ordinal)
			.Replace("\\r", "\r", StringComparison.Ordinal)
			.Replace("\\T", "\t", StringComparison.Ordinal)
			.Replace("\\t", "\t", StringComparison.Ordinal);
	}
}