// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		/// Matches every character that is not allowed in a loaded text line.
		/// Allowed characters are letters, digits, space, underscore, asterisk, dollar sign,
		/// comma, caret and hyphen.
		/// This includes accented letters as single Unicode letters, for example in French words
		/// such as français, école, crème brûlée and cœur.
		/// Combining mark characters are removed, so decomposed accents are not preserved.
		/// Used by <see cref="CleanLine"/> to remove unsupported characters from input files.
		/// </summary>
		private readonly Regex _invalidCharsRegex = new(
			@"[^\p{L}\p{N} _*\$,\^-]",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

		private string[] ReadArray(string path)
		{
			if (!File.Exists(path))
			{
				RuntimeHandler.Runtime.Log($"File not found: {path}");
				return [];
			}

			return [.. File
				.ReadLines(path, Encoding.UTF8)
				.Where(line => !IsCommentLine(line))
				.Select(CleanLine)];
		}

		private static string[] StripEndMarker(string[] lines)
		{
			return [.. lines.Where(line => !IsEndMarker(line))];
		}

		private static bool IsCommentLine(string line)
		{
			return line.TrimStart().StartsWith('#');
		}

		private static bool IsEndMarker(string line)
		{
			return line == "*END";
		}

		private string CleanLine(string line)
		{
			return _invalidCharsRegex
				.Replace(line, string.Empty)
				.Trim();
		}

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

			return File.Exists(path) ? path : null;
		}

		private static string GetDefaultPath(string filename)
		{
			return Path.Combine(
				Settings.Instance.DataDirectory,
				Path.ChangeExtension(filename, ".TXT"));
		}
	}
}