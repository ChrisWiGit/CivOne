// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CivOne.Services;

namespace CivOne.IO
{
	internal interface ITextFileLoader
	{
		string[] LoadArray(string filename);
	}
	internal partial class TextFileLoader : ITextFileLoader
	{
		[GeneratedRegex(@"[^a-zA-Z0-9 _-]")]
		private static partial Regex InvalidCharsRegex();

		public string[] LoadArray(string filename)
		{
			var path = Path.Combine(
				Settings.Instance.DataDirectory,
				Path.ChangeExtension(filename, ".TXT"));

			if (!File.Exists(path))
			{
				RuntimeHandler.Runtime.Log($"File not found: {path}");
				return [];
			}

			return [.. File.ReadLines(path).Select(line => InvalidCharsRegex().Replace(line, "").Trim())];
		}
	}

	internal static class TextFileFactory
	{
		private static ITextFileLoader _loader;
		private static TextFile _gameTexts;


		public static IGameTexts Get(bool reload = false)
		{
			_loader ??= new TextFileLoader();

			if (_gameTexts == null)
			{
				_gameTexts = new(_loader);
				_gameTexts.Reset();
				TranslationServiceFactory.RegisterLanguageObserver(_gameTexts);
			}

			if (reload)
			{
				_gameTexts.Reset();
			}

			return _gameTexts;
		}

		public static ITextFileLoader GetLoader()
		{
			_loader ??= new TextFileLoader();
			return _loader;
		}

		public static string[] LoadTextFile(string filename) => GetLoader().LoadArray(filename);

		public static TextFile GetInstance(bool reload = false) => _ = Get(reload) as TextFile;

		public static void ClearInstance()
		{
			TranslationServiceFactory.UnregisterLanguageObserver(_gameTexts);
			_gameTexts = null;
			_loader = null;
		}
	}

	internal interface IGameTexts
	{
		string[] GetGameText(string key);
	}

	internal interface IGameTextsCommand : IGameTexts, ITranslationLanguageObserver
	{
		void Reset();
	}


	internal class TextFile : IGameTextsCommand
	{
		private readonly string[] TEXT_FILES = ["BLURB0", "BLURB1", "BLURB2", "BLURB3", "BLURB4", "ERROR", "HELP", "KING", "PRODUCE"];
		private readonly Dictionary<string, string[]> _gameTexts = [];

		private readonly ITextFileLoader _textFileLoader;

		public TextFile(ITextFileLoader textFileLoader)
		{
			_textFileLoader = textFileLoader;
		}

		public string[] GetGameText(string key)
		{
			if (_gameTexts.TryGetValue(key, out string[] value))
			{
				return value;
			}
			return [];
		}

		public string[] LoadArray(string filename) => _textFileLoader.LoadArray(filename);

		public void OnLanguageChanged(string activeLanguagePostfix)
		{
			Reset();
		}

		public void Reset()
		{
			_gameTexts.Clear();
			foreach (string file in TEXT_FILES)
			{
				AddTextEntries(file, _textFileLoader.LoadArray(file));
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