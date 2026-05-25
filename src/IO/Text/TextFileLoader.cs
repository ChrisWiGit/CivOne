// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using CivOne.Services;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Default loader for Civilization text files.
	/// Resolves localized files first, then falls back to original data files.
	/// </summary>
	internal class TextFileLoader : ITextFileLoader
	{
		/// <summary>
		/// Loads and sanitizes a text file by logical name.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// Example: HELP.
		/// </param>
		/// <returns>
		/// Cleaned lines from localized or fallback file.
		/// Returns an empty array if neither file exists.
		/// </returns>
		public string[] LoadArray(string filename)
		{
			var localizedPath = GetLocalizedPath(filename);
			var defaultPath = GetDefaultPath(filename);

			if (localizedPath == null)
			{
				return ReadArray(defaultPath);
			}

			string[] localizedLines = StripEndMarker(ReadArray(localizedPath));
			string[] defaultLines = ReadArray(defaultPath);

			if (defaultLines.Length == 0)
			{
				return localizedLines;
			}

			return [.. localizedLines, .. defaultLines];
		}

		/// <summary>
		/// Reads a text file, removes comments, and normalizes each line.
		/// </summary>
		/// <param name="path">
		/// Absolute file path to read.
		/// </param>
		/// <returns>
		/// Cleaned file lines, or an empty array when the file is missing.
		/// </returns>
		private string[] ReadArray(string path)
		{
			if (!File.Exists(path))
			{
				RuntimeHandler.Runtime.Log($"File not found: {path}");
				return [];
			}

			return [.. File
				.ReadLines(path, Encoding.UTF8)
			.Where(line => !IsCommentLine(line))];
		}

		/// <summary>
		/// Removes the end marker line from a localized file.
		/// </summary>
		/// <param name="lines">
		/// Raw lines from the localized file.
		/// </param>
		/// <returns>
		/// All lines except <c>*END</c>.
		/// </returns>
		private static string[] StripEndMarker(string[] lines)
		{
			return [.. lines.Where(line => !IsEndMarker(line))];
		}

		/// <summary>
		/// Detects comment lines in text files.
		/// </summary>
		/// <param name="line">
		/// Input line.
		/// </param>
		/// <returns>
		/// <see langword="true"/> when the line starts with <c>#</c> after trimming leading whitespace.
		/// </returns>
		private static bool IsCommentLine(string line)
		{
			return line.TrimStart().StartsWith('#');
		}

		/// <summary>
		/// Detects the explicit end marker used by localized Civ text files.
		/// </summary>
		/// <param name="line">
		/// Input line.
		/// </param>
		/// <returns>
		/// <see langword="true"/> when the line equals <c>*END</c>.
		/// </returns>
		private static bool IsEndMarker(string line)
		{
			return line == "*END";
		}

		/// <summary>
		/// Resolves the localized translation file if a language is active.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// </param>
		/// <returns>
		/// The matching localized path, or <see langword="null"/> when no active language exists or the file is missing.
		/// </returns>
		private static string? GetLocalizedPath(string filename)
		{
			var postfix = TranslationServiceFactory.ActiveLanguagePostfix;

			if (string.IsNullOrEmpty(postfix))
			{
				return null;
			}

			var path = Path.Combine(
				RuntimeHandler.Runtime.StorageDirectory,
				"translations",
				$"{filename}_{postfix}.txt");

			return ResolveExistingFilePath(path);
		}

		/// <summary>
		/// Resolves the fallback data file path.
		/// </summary>
		/// <remarks>
		/// The original game data uses upper-case filenames, but Linux file systems are case-sensitive.
		/// This method therefore accepts whichever casing exists on disk and returns the real file path.
		/// If no matching file is found, the conventional upper-case path is returned so the caller still
		/// logs the expected location.
		/// </remarks>
		/// <param name="filename">
		/// Logical file name without extension.
		/// </param>
		/// <returns>
		/// The resolved fallback file path.
		/// </returns>
		private static string GetDefaultPath(string filename)
		{
			var path = Path.Combine(
				Settings.Instance.DataDirectory,
				Path.ChangeExtension(filename, ".TXT"));

			return ResolveExistingFilePath(path) ?? path;
		}

		/// <summary>
		/// Finds the on-disk file path using case-insensitive name matching.
		/// </summary>
		/// <remarks>
		/// This avoids failures on Linux when files exist as <c>KING.txt</c> or <c>king.txt</c>
		/// while the code asks for <c>KING.TXT</c>.
		/// </remarks>
		/// <param name="path">
		/// Preferred file path.
		/// </param>
		/// <returns>
		/// The actual file path on disk, or <see langword="null"/> if no case-insensitive match exists.
		/// </returns>
		private static string? ResolveExistingFilePath(string path)
		{
			string directory = Path.GetDirectoryName(path) ?? string.Empty;
			string fileName = Path.GetFileName(path);

			if (!Directory.Exists(directory))
			{
				return null;
			}

			return Directory
				.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
				.FirstOrDefault(filePath => string.Equals(Path.GetFileName(filePath), fileName, StringComparison.OrdinalIgnoreCase));
		}
	}
}