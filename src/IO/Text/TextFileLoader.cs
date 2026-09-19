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
				return ReadArray(defaultPath, isOriginalFallback: true);
			}

			string[] localizedRawLines = ReadArray(localizedPath, isOriginalFallback: false);
			bool hasEndMarker = HasEndMarker(localizedRawLines);
			string[] localizedLines = hasEndMarker ? StripEndMarker(localizedRawLines) : localizedRawLines;

			if (!hasEndMarker)
			{
				return localizedLines;
			}

			string[] defaultLines = ReadArray(defaultPath, isOriginalFallback: true);

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
		private static string[] ReadArray(string path, bool isOriginalFallback)
		{
			if (!File.Exists(path))
			{
				RuntimeHandler.Runtime.Log($"File not found: {path}");
				return [];
			}

			if (!isOriginalFallback)
			{
				return [.. File
					.ReadLines(path, Encoding.UTF8)
				.Where(line => !IsCommentLine(line))];
			}

			byte[] bytes = File.ReadAllBytes(path);
			string? decodedText = DecodeOriginalFallbackText(path, bytes);
			return [.. SplitLines(decodedText ?? string.Empty).Where(line => !IsCommentLine(line))];
		}

		private static string? DecodeOriginalFallbackText(string path, byte[] bytes)
		{
			if (TryDecodeStrictUtf8(bytes, out string? utf8Text))
			{
				return utf8Text;
			}

			Encoding? cp437 = TryGetCodePageEncoding(437);
			if (cp437 != null)
			{
				RuntimeHandler.Runtime.Log($"Decoding original fallback text as CP437: {path}");
				return cp437.GetString(bytes);
			}

			RuntimeHandler.Runtime.Log($"CP437 unavailable, decoding original fallback text with built-in DOS-Western fallback: {path}");
			return DecodeAsDosWesternFallback(bytes);
		}

		private static bool TryDecodeStrictUtf8(byte[] bytes, out string? decoded)
		{
			decoded = null;
			try
			{
				decoded = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
				return true;
			}
			catch (DecoderFallbackException)
			{
				return false;
			}
		}

		private static Encoding? TryGetCodePageEncoding(int codePage)
		{
			try
			{
				return Encoding.GetEncoding(codePage);
			}
			catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
			{
				return null;
			}
		}

		private static string DecodeAsDosWesternFallback(byte[] bytes)
		{
			char[] output = new char[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				output[i] = bytes[i] switch
				{
					0x80 => '\u00C7',
					0x81 => '\u00FC',
					0x82 => '\u00E9',
					0x83 => '\u00E2',
					0x84 => '\u00E4',
					0x85 => '\u00E0',
					0x87 => '\u00E7',
					0x88 => '\u00EA',
					0x89 => '\u00EB',
					0x8A => '\u00E8',
					0x8B => '\u00EF',
					0x8C => '\u00EE',
					0x8D => '\u00EC',
					0x8E => '\u00C4',
					0x93 => '\u00F4',
					0x94 => '\u00F6',
					0x95 => '\u00F2',
					0x96 => '\u00FB',
					0x99 => '\u00D6',
					0x9A => '\u00DC',
					0xE1 => '\u00DF',
					_ => (char)bytes[i]
				};
			}

			return new string(output);
		}

		private static string[] SplitLines(string text)
		{
			string normalized = text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace('\r', '\n');
			return normalized.Split('\n');
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
		/// Checks whether a localized text file explicitly requests fallback merge using <c>*END</c>.
		/// </summary>
		/// <param name="lines">
		/// Raw localized lines.
		/// </param>
		/// <returns>
		/// <see langword="true"/> when any line equals <c>*END</c>.
		/// </returns>
		private static bool HasEndMarker(string[] lines)
		{
			return lines.Any(IsEndMarker);
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
			return string.Equals(line.Trim(), "*END", StringComparison.Ordinal);
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