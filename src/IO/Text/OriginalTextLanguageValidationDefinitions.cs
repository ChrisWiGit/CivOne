namespace CivOne.IO.Text
{
	/// <summary>
	/// One file-level validation definition.
	/// </summary>
	/// <param name="FileName">
	/// The target file name that should be validated.
	/// </param>
	/// <param name="MinimumMatches">
	/// The minimum number of matching readable segments required for verification.
	/// </param>
	/// <param name="MaximumMismatches">
	/// The maximum number of segment mismatches allowed for verification.
	/// </param>
	/// <param name="Segments">
	/// The segment definitions used to validate the file content.
	/// </param>
	internal sealed record OriginalTextLanguageValidationFileDefinition(
		string FileName,
		int MinimumMatches,
		int MaximumMismatches,
		OriginalTextLanguageValidationSegmentDefinition[] Segments);

	/// <summary>
	/// One segment-level validation definition.
	/// </summary>
	/// <param name="SegmentId">
	/// A stable segment identifier used for diagnostics and traceability.
	/// </param>
	/// <param name="ExpectedSha256">
	/// The expected SHA-256 hash in uppercase hexadecimal format.
	/// </param>
	/// <param name="Source">
	/// The source type that defines how the segment is read from the file.
	/// </param>
	/// <param name="Marker">
	/// The marker key used for marker-based reads.
	/// Null when marker lookup is not required.
	/// </param>
	/// <param name="BodyLineIndex">
	/// The zero-based line index within the marker body used for hashing.
	/// </param>
	/// <param name="Offset">
	/// The zero-based byte offset for byte-window reads.
	/// </param>
	/// <param name="Length">
	/// The number of bytes to read for byte-window segment hashing.
	/// </param>
	internal sealed record OriginalTextLanguageValidationSegmentDefinition(
		string SegmentId,
		string ExpectedSha256,
		OriginalTextLanguageSegmentSource Source,
		string? Marker = null,
		int BodyLineIndex = 0,
		int Offset = 0,
		int Length = 0);

	/// <summary>
	/// Supported sources for validation segments.
	/// </summary>
	internal enum OriginalTextLanguageSegmentSource
	{
		/// <summary>
		/// Reads one text line from a parsed marker section.
		/// </summary>
		MarkerBodyLine,
		/// <summary>
		/// Reads a fixed byte window from the raw file content.
		/// </summary>
		ByteWindow
	}
}