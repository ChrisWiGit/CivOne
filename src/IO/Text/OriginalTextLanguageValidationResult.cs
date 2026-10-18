using System.Collections.Generic;
using System.Linq;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Aggregate validation result for all required text files.
	/// </summary>
	internal sealed record OriginalTextLanguageValidationResult(
		bool IsVerified,
		IReadOnlyList<OriginalTextLanguageValidationFileResult> Files,
		IReadOnlyList<OriginalTextLanguageValidationFileResult> FilesWithMismatches)
	{
		/// <summary>
		/// Creates a result and derives mismatching files automatically.
		/// </summary>
		public static OriginalTextLanguageValidationResult Create(IReadOnlyList<OriginalTextLanguageValidationFileResult> files)
		{
			IReadOnlyList<OriginalTextLanguageValidationFileResult> mismatches = [.. files.Where(x => !x.IsVerified)];
			return new OriginalTextLanguageValidationResult(mismatches.Count == 0, files, mismatches);
		}
	}

	/// <summary>
	/// Per-file validation counters.
	/// </summary>
	internal sealed record OriginalTextLanguageValidationFileResult(
		string FileName,
		int TotalSegments,
		int ReadableSegments,
		int MatchedSegments,
		int MismatchedSegments,
		int MinimumMatches,
		int MaximumMismatches,
		bool IsVerified);
}