using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Hash-based validation for the original fallback text files.
	/// </summary>
	internal sealed class OriginalTextLanguageValidationService : IOriginalTextLanguageValidationService
	{
		private readonly IReadOnlyList<OriginalTextLanguageValidationFileDefinition> _definitions;

		/// <summary>
		/// Initializes a new instance of the validation service.
		/// </summary>
		/// <param name="definitions">
		/// Optional custom validation definitions.
		/// When null, the built-in default definitions are used.
		/// </param>
		internal OriginalTextLanguageValidationService(IReadOnlyList<OriginalTextLanguageValidationFileDefinition>? definitions = null)
		{
			_definitions = definitions ?? OriginalTextLanguageValidationDefaultDefinitions.Definitions;
		}

		/// <summary>
		/// Validates known original text files in the specified data directory
		/// by comparing hashed content segments against expected values.
		/// </summary>
		/// <param name="dataDirectory">
		/// The directory that contains the original Civilization text files.
		/// </param>
		/// <returns>
		/// A validation result containing per-file verification details and
		/// the aggregated validation outcome.
		/// </returns>
		public OriginalTextLanguageValidationResult Validate(string dataDirectory)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

			List<OriginalTextLanguageValidationFileResult> fileResults = [];
			foreach (OriginalTextLanguageValidationFileDefinition definition in _definitions)
			{
				fileResults.Add(ValidateFile(dataDirectory, definition));
			}

			return OriginalTextLanguageValidationResult.Create(fileResults);
		}

		/// <summary>
		/// Validates one configured file and computes segment-level match statistics.
		/// </summary>
		/// <param name="dataDirectory">
		/// The base directory that contains the text files to validate.
		/// </param>
		/// <param name="definition">
		/// The validation definition that describes which file and segments to verify.
		/// </param>
		/// <returns>
		/// A per-file validation result with counts for readable, matched,
		/// and mismatched segments.
		/// </returns>
		private static OriginalTextLanguageValidationFileResult ValidateFile(string dataDirectory, OriginalTextLanguageValidationFileDefinition definition)
		{
			string? resolvedPath = FileSystem.FindFileIgnoreCase(dataDirectory, definition.FileName)
				?? Path.Combine(dataDirectory, definition.FileName);

			if (!File.Exists(resolvedPath))
			{
				return new OriginalTextLanguageValidationFileResult(
					FileName: definition.FileName,
					TotalSegments: definition.Segments.Length,
					ReadableSegments: 0,
					MatchedSegments: 0,
					MismatchedSegments: 0,
					MinimumMatches: definition.MinimumMatches,
					MaximumMismatches: definition.MaximumMismatches,
					IsVerified: false);
			}

			byte[] rawBytes = File.ReadAllBytes(resolvedPath);
			Dictionary<string, string[]> entries = ParseEntries(rawBytes);

			int readableSegments = 0;
			int matchedSegments = 0;
			int mismatchedSegments = 0;

			foreach (OriginalTextLanguageValidationSegmentDefinition segment in definition.Segments)
			{
				if (!TryReadSegment(rawBytes, entries, segment, out string? segmentHash))
				{
					continue;
				}

				readableSegments++;
				if (string.Equals(segmentHash, segment.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
				{
					matchedSegments++;
				}
				else
				{
					mismatchedSegments++;
				}
			}

			bool isVerified = readableSegments >= definition.MinimumMatches
				&& matchedSegments >= definition.MinimumMatches
				&& mismatchedSegments <= definition.MaximumMismatches;

			return new OriginalTextLanguageValidationFileResult(
				FileName: definition.FileName,
				TotalSegments: definition.Segments.Length,
				ReadableSegments: readableSegments,
				MatchedSegments: matchedSegments,
				MismatchedSegments: mismatchedSegments,
				MinimumMatches: definition.MinimumMatches,
				MaximumMismatches: definition.MaximumMismatches,
				IsVerified: isVerified);
		}

		/// <summary>
		/// Reads a segment from the source file content and computes its SHA-256 hash.
		/// </summary>
		/// <param name="rawBytes">
		/// The raw file bytes used for byte-window segment reads.
		/// </param>
		/// <param name="entries">
		/// Parsed marker-to-body mapping used for marker-based segment reads.
		/// </param>
		/// <param name="segment">
		/// The segment definition describing source, bounds, and marker metadata.
		/// </param>
		/// <param name="segmentHash">
		/// When this method returns true, contains the computed uppercase hexadecimal hash.
		/// Otherwise, contains null.
		/// </param>
		/// <returns>
		/// True when the segment could be read and hashed;
		/// otherwise false.
		/// </returns>
		private static bool TryReadSegment(
			byte[] rawBytes,
			Dictionary<string, string[]> entries,
			OriginalTextLanguageValidationSegmentDefinition segment,
			out string? segmentHash)
		{
			segmentHash = null;
			switch (segment.Source)
			{
				case OriginalTextLanguageSegmentSource.MarkerBodyLine:
					if (segment.Marker == null || !entries.TryGetValue(segment.Marker, out string[]? lines))
					{
						return false;
					}

					if (segment.BodyLineIndex < 0 || segment.BodyLineIndex >= lines.Length)
					{
						return false;
					}

					string normalized = NormalizeSegmentText(lines[segment.BodyLineIndex]);
					if (normalized.Length == 0)
					{
						return false;
					}

					segmentHash = ComputeSha256Hex(Encoding.UTF8.GetBytes(normalized));
					return true;
				case OriginalTextLanguageSegmentSource.ByteWindow:
					if (segment.Offset < 0 || segment.Length <= 0 || segment.Offset + segment.Length > rawBytes.Length)
					{
						return false;
					}

					byte[] window = rawBytes[segment.Offset..(segment.Offset + segment.Length)];
					segmentHash = ComputeSha256Hex(window);
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parses marker-based text entries from raw file bytes.
		/// </summary>
		/// <param name="rawBytes">
		/// The raw file bytes, optionally including a UTF-8 BOM.
		/// </param>
		/// <returns>
		/// A case-insensitive marker-to-body-lines map parsed from the text structure.
		/// </returns>
		private static Dictionary<string, string[]> ParseEntries(byte[] rawBytes)
		{
			Dictionary<string, string[]> output = new(StringComparer.OrdinalIgnoreCase);

			if (rawBytes.Length >= 3
				&& rawBytes[0] == 0xEF
				&& rawBytes[1] == 0xBB
				&& rawBytes[2] == 0xBF)
			{
				rawBytes = rawBytes[3..];
			}

			string text = Encoding.Latin1.GetString(rawBytes);
			string[] lines = SplitLines(text);
			int index = 0;

			while (index < lines.Length)
			{
				if (!IsMarkerLine(lines[index]))
				{
					index++;
					continue;
				}

				if (IsEndMarker(lines[index]))
				{
					break;
				}

				List<string> markers = [];
				while (index < lines.Length && IsMarkerLine(lines[index]) && !IsEndMarker(lines[index]))
				{
					string marker = lines[index][1..].Trim();
					if (!string.IsNullOrEmpty(marker))
					{
						markers.Add(marker);
					}
					index++;
				}

				List<string> body = [];
				while (index < lines.Length && lines[index].Length > 0 && !IsMarkerLine(lines[index]))
				{
					body.Add(lines[index]);
					index++;
				}

				if (body.Count == 0)
				{
					continue;
				}

				string[] bodyLines = [.. body];
				foreach (string marker in markers)
				{
					if (!output.ContainsKey(marker))
					{
						output[marker] = bodyLines;
					}
				}
			}

			return output;
		}

		/// <summary>
		/// Splits text into logical lines while normalizing line endings.
		/// </summary>
		/// <param name="text">
		/// The input text to split.
		/// </param>
		/// <returns>
		/// The normalized lines as an array.
		/// </returns>
		private static string[] SplitLines(string text)
		{
			string normalized = text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace('\r', '\n');
			string[] lines = normalized.Split('\n');
			if (lines.Length > 0)
			{
				lines[0] = lines[0].TrimStart('\uFEFF');
			}
			return lines;
		}

		/// <summary>
		/// Determines whether a line is a marker line.
		/// </summary>
		/// <param name="line">
		/// The line to inspect.
		/// </param>
		/// <returns>
		/// True when the line starts with '*';
		/// otherwise false.
		/// </returns>
		private static bool IsMarkerLine(string line) => line.StartsWith('*');

		/// <summary>
		/// Determines whether a line marks the end of marker-based content.
		/// </summary>
		/// <param name="line">
		/// The line to inspect.
		/// </param>
		/// <returns>
		/// True when the trimmed line equals "*END" using ordinal comparison;
		/// otherwise false.
		/// </returns>
		private static bool IsEndMarker(string line) => string.Equals(line.Trim(), "*END", StringComparison.Ordinal);

		/// <summary>
		/// Normalizes segment text before hashing to reduce format-related variance.
		/// </summary>
		/// <param name="value">
		/// The raw segment text.
		/// </param>
		/// <returns>
		/// The normalized uppercase text with collapsed whitespace.
		/// </returns>
		private static string NormalizeSegmentText(string value)
		{
			string expanded = value.Replace('^', '\n');
			string[] words = expanded
				.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			string compact = string.Join(" ", words);
			return compact.Trim().ToUpperInvariant();
		}

		/// <summary>
		/// Computes an uppercase hexadecimal SHA-256 hash for the specified bytes.
		/// </summary>
		/// <param name="value">
		/// The bytes to hash.
		/// </param>
		/// <returns>
		/// The SHA-256 hash as an uppercase hexadecimal string.
		/// </returns>
		private static string ComputeSha256Hex(byte[] value)
		{
			return Convert.ToHexString(SHA256.HashData(value));
		}
	}
}