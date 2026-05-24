// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
using System.Collections.Generic;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Parses Civ text files and exposes entries by composite key.
	/// Composite keys use FILE/MARKER format, for example ERROR/DEMOCRACY.
	/// </summary>
	internal class TextFile : IGameTextsCommand
	{
		private readonly string[] TEXT_FILES = ["BLURB0", "BLURB1", "BLURB2", "BLURB3", "BLURB4", "ERROR", "HELP", "KING", "PRODUCE"];
		private readonly Dictionary<string, string[]> _gameTexts = [];

		private readonly ITextFileLoader _textFileLoader;

		/// <summary>
		/// Creates a parser with an injected line loader.
		/// </summary>
		/// <param name="textFileLoader">
		/// Loader used to read source files.
		/// </param>
		public TextFile(ITextFileLoader textFileLoader)
		{
			_textFileLoader = textFileLoader;
		}

		/// <summary>
		/// Returns parsed text lines for one composite key.
		/// </summary>
		/// <param name="key">
		/// Composite key in format FILE/MARKER.
		/// </param>
		/// <returns>
		/// Matching lines.
		/// Returns an empty array when key is not present.
		/// </returns>
		public string[] GetGameText(string key)
		{
			if (_gameTexts.TryGetValue(key, out string[] value))
			{
				return value;
			}
			return [];
		}

		/// <summary>
		/// Delegates raw file loading to the configured loader.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// </param>
		/// <returns>
		/// Cleaned file lines.
		/// </returns>
		public string[] LoadArray(string filename) => _textFileLoader.LoadArray(filename);

		/// <summary>
		/// Handles language-change notifications by rebuilding cached entries.
		/// </summary>
		/// <param name="activeLanguagePostfix">
		/// Active language postfix.
		/// Not used directly because full cache reload is required.
		/// </param>
		public void OnLanguageChanged(string activeLanguagePostfix)
		{
			Reset();
		}

		/// <summary>
		/// Clears cache and reloads all configured source files.
		/// </summary>
		public void Reset()
		{
			_gameTexts.Clear();
			foreach (string file in TEXT_FILES)
			{
				var text = _textFileLoader.LoadArray(file);
				AddTextEntries(file, text);
			}
		}

		private void AddTextEntries(string file, string[] textFile)
		{
			int index = 0;
			while (index < textFile.Length)
			{
				if (!IsMarkerLine(textFile[index]))
				{
					index++;
					continue;
				}

				if (IsEndMarker(textFile[index]))
				{
					break;
				}

				ParseEntry(textFile, ref index, out List<string> keys, out List<string> lines);
				AddEntryLines(file, keys, lines);
			}
		}

		private static void ParseEntry(string[] textFile, ref int index, out List<string> keys, out List<string> lines)
		{
			keys = [];
			lines = [];

			while (index < textFile.Length && IsMarkerLine(textFile[index]) && !IsEndMarker(textFile[index]))
			{
				keys.Add(textFile[index][1..]);
				index++;
			}

			while (index < textFile.Length && textFile[index].Length > 0 && !IsMarkerLine(textFile[index]))
			{
				lines.Add(textFile[index]);
				index++;
			}
		}

		private void AddEntryLines(string file, List<string> keys, List<string> lines)
		{
			if (lines.Count == 0)
			{
				return;
			}

			string[] lineArray = [.. lines];
			foreach (string key in keys)
			{
				string compositeKey = $"{file}/{key}";
				_ = _gameTexts.TryAdd(compositeKey, lineArray);
			}
		}

		private static bool IsMarkerLine(string line) => line.StartsWith('*');

		private static bool IsEndMarker(string line) => line == "*END";
	}
}