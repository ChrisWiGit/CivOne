using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CivOne.Services.Translation
{
	/// <summary>
	/// Default file-system based implementation of <see cref="ITranslationFileRepository"/>.
	/// Reads translation files from the runtime translation directory and validates file structure.
	/// </summary>
	public class TranslationFileRepositoryImpl : ITranslationFileRepository
	{
		private const string EqualsPlaceholder = "[EQ]";

		/// <inheritdoc/>
		public IReadOnlyList<TranslationLanguageInfo> GetAvailableLanguages(string storageDirectory, Action<string> log = null)
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
			foreach (string filePath in Directory.EnumerateFiles(translationDirectory, "*.txt", SearchOption.TopDirectoryOnly)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(filePath);
				if (!TryGetPostfix(fileName, out string postfix))
				{
					continue;
				}

				if (!TryLoadTranslations(filePath, out _, out string error))
				{
					log?.Invoke($"Skipping translation file '{fileName}': {error}");
					continue;
				}

				output.Add(new TranslationLanguageInfo(postfix, filePath));
			}

			return output;
		}

		/// <inheritdoc/>
		public bool TryLoadTranslations(string filePath, out IReadOnlyDictionary<string, string> translations, out string error)
		{
			translations = null;
			error = null;

			if (string.IsNullOrWhiteSpace(filePath))
			{
				error = "File path is empty.";
				return false;
			}

			if (!File.Exists(filePath))
			{
				error = $"File not found: {filePath}";
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
						error = $"Malformed line {i + 1}: missing '=' separator.";
						return false;
					}

					string key = NormalizeKey(UnescapeControlCharacters(UnescapeEquals(line[..separatorIndex])));
					if (key.Length == 0)
					{
						error = $"Malformed line {i + 1}: empty key.";
						return false;
					}

					string value = UnescapeControlCharacters(UnescapeEquals(line[(separatorIndex + 1)..]));
					output[key] = value;
				}
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}

			translations = output;
			return true;
		}

		private static bool TryGetPostfix(string fileName, out string postfix)
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

		private static string NormalizeKey(string key) => key?.Trim().ToUpperInvariant() ?? string.Empty;

		private static string UnescapeEquals(string value) => value.Replace(EqualsPlaceholder, "=", StringComparison.Ordinal);

		private static string UnescapeControlCharacters(string value) => value
			.Replace("\\r\\n", "\r\n", StringComparison.Ordinal)
			.Replace("\\n", "\n", StringComparison.Ordinal)
			.Replace("\\r", "\r", StringComparison.Ordinal)
			.Replace("\\t", "\t", StringComparison.Ordinal);
	}
}