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
		private readonly Regex _invalidCharsRegex = new(
			@"[^a-zA-Z0-9 _-]",
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
			var path = GetLocalizedPath(filename) ?? GetDefaultPath(filename);

			if (!File.Exists(path))
			{
				RuntimeHandler.Runtime.Log($"File not found: {path}");
				return [];
			}

			return [.. File
				.ReadLines(path, Encoding.UTF8)
				.Select(CleanLine)];
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