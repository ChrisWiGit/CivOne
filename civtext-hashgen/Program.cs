using System.Globalization;
using System.Security.Cryptography;
using System.Text;

if (args.Length == 0 || HasHelpSwitch(args))
{
	PrintHelp();
	return args.Length == 0 ? 1 : 0;
}

if (!TryParseArguments(args, out string dataDirectory, out bool traceEnabled, out string? outputPath, out string error))
{
	Console.Error.WriteLine($"Error: {error}");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

string fullDataDirectory = Path.GetFullPath(dataDirectory);
if (!Directory.Exists(fullDataDirectory))
{
	Console.Error.WriteLine($"Error: Directory does not exist: {fullDataDirectory}");
	return 2;
}

GenerationResult result = Generate(fullDataDirectory, traceEnabled);
if (result.Errors.Count > 0)
{
	Console.Error.WriteLine("Generation failed.");
	foreach (string line in result.Errors)
	{
		Console.Error.WriteLine($"  - {line}");
	}
	return 3;
}

if (string.IsNullOrWhiteSpace(outputPath))
{
	Console.Write(result.GeneratedCode);
}
else
{
	string fullOutputPath = Path.GetFullPath(outputPath);
	string? directory = Path.GetDirectoryName(fullOutputPath);
	if (!string.IsNullOrWhiteSpace(directory))
	{
		Directory.CreateDirectory(directory);
	}

	File.WriteAllText(fullOutputPath, result.GeneratedCode, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	Console.Error.WriteLine("Wrote generated definitions to: " + fullOutputPath);
}

return 0;

static bool HasHelpSwitch(string[] args)
	=> args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

static bool TryParseArguments(string[] args, out string dataDirectory, out bool traceEnabled, out string? outputPath, out string error)
{
	dataDirectory = string.Empty;
	traceEnabled = false;
	outputPath = null;
	error = string.Empty;

	List<string> positionals = [];
	for (int index = 0; index < args.Length; index++)
	{
		string arg = args[index];

		if (arg.Equals("--trace", StringComparison.OrdinalIgnoreCase))
		{
			traceEnabled = true;
			continue;
		}

		if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase))
		{
			if (index + 1 >= args.Length)
			{
				error = "--output requires a file path.";
				return false;
			}

			outputPath = args[++index];
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				error = "--output value must not be empty.";
				return false;
			}
			continue;
		}

		if (arg.StartsWith('-'))
		{
			error = $"Unknown option: {arg}";
			return false;
		}

		positionals.Add(arg);
	}

	if (positionals.Count != 1)
	{
		error = "Exactly one data directory path is required.";
		return false;
	}

	dataDirectory = positionals[0];
	return true;
}

static GenerationResult Generate(string dataDirectory, bool traceEnabled)
{
	List<string> errors = [];
	List<GeneratedFileDefinition> generatedFiles = [];

	foreach (FileDefinitionTemplate template in GetTemplates())
	{
		string? resolvedPath = ResolveFilePath(dataDirectory, template.FileName);
		if (resolvedPath == null)
		{
			errors.Add($"Missing source file for '{template.FileName}'.");
			continue;
		}

		byte[] rawBytes = File.ReadAllBytes(resolvedPath);
		Dictionary<string, string[]> entries = ParseEntries(rawBytes);
		List<GeneratedSegmentDefinition> generatedSegments = [];

		foreach (SegmentTemplate segment in template.Segments)
		{
			if (!TryReadSegment(rawBytes, entries, segment, out SegmentReadResult? segmentResult))
			{
				errors.Add($"Could not read segment '{segment.SegmentId}' in '{template.FileName}'.");
				continue;
			}

			if (segmentResult == null)
			{
				errors.Add($"Segment '{segment.SegmentId}' in '{template.FileName}' returned no hash result.");
				continue;
			}

			generatedSegments.Add(new GeneratedSegmentDefinition(segment, segmentResult));
			if (traceEnabled)
			{
				PrintTrace(template.FileName, segment, segmentResult);
			}
		}

		generatedFiles.Add(new GeneratedFileDefinition(template, generatedSegments));
	}

	if (errors.Count > 0)
	{
		return new GenerationResult(string.Empty, errors);
	}

	string generatedCode = BuildCode(generatedFiles);
	return new GenerationResult(generatedCode, errors);
}

static string? ResolveFilePath(string dataDirectory, string expectedFileName)
{
	return Directory
		.EnumerateFiles(dataDirectory)
		.FirstOrDefault(path => string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase));
}

static bool TryReadSegment(
	byte[] rawBytes,
	Dictionary<string, string[]> entries,
	SegmentTemplate segment,
	out SegmentReadResult? result)
{
	result = null;

	switch (segment.Source)
	{
		case SegmentSource.MarkerBodyLine:
			if (segment.Marker == null || !entries.TryGetValue(segment.Marker, out string[]? lines))
			{
				return false;
			}

			if (segment.BodyLineIndex < 0 || segment.BodyLineIndex >= lines.Length)
			{
				return false;
			}

			string rawLine = lines[segment.BodyLineIndex];
			string normalized = NormalizeSegmentText(rawLine);
			if (normalized.Length == 0)
			{
				return false;
			}

			string markerHash = ComputeSha256Hex(Encoding.UTF8.GetBytes(normalized));
			result = new SegmentReadResult(
				Hash: markerHash,
				RawPreview: rawLine,
				NormalizedPreview: normalized,
				FirstTenWords: FirstWords(normalized, 10));
			return true;

		case SegmentSource.ByteWindow:
			if (segment.Offset < 0 || segment.Length <= 0 || segment.Offset + segment.Length > rawBytes.Length)
			{
				return false;
			}

			byte[] window = rawBytes[segment.Offset..(segment.Offset + segment.Length)];
			string windowHash = ComputeSha256Hex(window);
			result = new SegmentReadResult(
				Hash: windowHash,
				RawPreview: ToHexPreview(window, 16),
				NormalizedPreview: "(n/a byte window)",
				FirstTenWords: "(n/a byte window)");
			return true;

		default:
			return false;
	}
}

static Dictionary<string, string[]> ParseEntries(byte[] rawBytes)
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

static string[] SplitLines(string text)
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

static bool IsMarkerLine(string line) => line.StartsWith('*');

static bool IsEndMarker(string line) => string.Equals(line.Trim(), "*END", StringComparison.Ordinal);

static string NormalizeSegmentText(string value)
{
	string expanded = value.Replace('^', '\n');
	string[] words = expanded
		.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
	string compact = string.Join(" ", words);
	return compact.Trim().ToUpperInvariant();
}

static string ComputeSha256Hex(byte[] value)
	=> Convert.ToHexString(SHA256.HashData(value));

static string FirstWords(string normalized, int wordCount)
{
	string[] words = normalized
		.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
	return words.Length <= wordCount
		? normalized
		: string.Join(" ", words.Take(wordCount));
}

static string ToHexPreview(byte[] bytes, int maxBytes)
{
	int count = Math.Min(maxBytes, bytes.Length);
	string head = Convert.ToHexString(bytes[..count]);
	return bytes.Length > count ? $"{head}..." : head;
}

static void PrintTrace(string fileName, SegmentTemplate segment, SegmentReadResult result)
{
	Console.Error.WriteLine(
		$"TRACE {fileName} | {segment.SegmentId} | {segment.Source} | First10={result.FirstTenWords} | Hash={result.Hash}");
}

static string BuildCode(IReadOnlyList<GeneratedFileDefinition> files)
{
	StringBuilder builder = new();
	builder.AppendLine("namespace CivOne.IO.Text");
	builder.AppendLine("{");
	builder.AppendLine("\tinternal static class OriginalTextLanguageValidationDefaultDefinitions");
	builder.AppendLine("\t{");
	builder.AppendLine("\t\tinternal static readonly OriginalTextLanguageValidationFileDefinition[] Definitions =");
	builder.AppendLine("\t\t[");

	for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
	{
		GeneratedFileDefinition file = files[fileIndex];
		builder.AppendLine("\t\t\tnew(");
		builder.AppendLine("\t\t\t\tFileName: \"" + Escape(file.Template.FileName) + "\",");
		builder.AppendLine("\t\t\t\tMinimumMatches: " + file.Template.MinimumMatches.ToString(CultureInfo.InvariantCulture) + ",");
		builder.AppendLine("\t\t\t\tMaximumMismatches: " + file.Template.MaximumMismatches.ToString(CultureInfo.InvariantCulture) + ",");
		builder.AppendLine("\t\t\t\tSegments:");
		builder.AppendLine("\t\t\t\t[");

		for (int segmentIndex = 0; segmentIndex < file.Segments.Count; segmentIndex++)
		{
			GeneratedSegmentDefinition generated = file.Segments[segmentIndex];
			SegmentTemplate segment = generated.Template;
			string hash = generated.Result.Hash;
			string suffix = segment.Source switch
			{
				SegmentSource.MarkerBodyLine =>
					$", Marker: \"{Escape(segment.Marker ?? string.Empty)}\", BodyLineIndex: {segment.BodyLineIndex.ToString(CultureInfo.InvariantCulture)}",
				SegmentSource.ByteWindow =>
					$", Offset: {segment.Offset.ToString(CultureInfo.InvariantCulture)}, Length: {segment.Length.ToString(CultureInfo.InvariantCulture)}",
				_ => string.Empty
			};

			string comma = segmentIndex < file.Segments.Count - 1 ? "," : string.Empty;
			builder.AppendLine(
				"\t\t\t\t\tnew(\""
				+ Escape(segment.SegmentId)
				+ "\", \""
				+ hash
				+ "\", OriginalTextLanguageSegmentSource."
				+ segment.Source.ToString()
				+ suffix
				+ ")"
				+ comma);
		}

		builder.AppendLine("\t\t\t\t])" + (fileIndex < files.Count - 1 ? "," : string.Empty));
	}

	builder.AppendLine("\t\t];");
	builder.AppendLine("\t}");
	builder.AppendLine("}");
	return builder.ToString();
}

static string Escape(string value)
	=> value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

static IReadOnlyList<FileDefinitionTemplate> GetTemplates() =>
[
	new("BLURB0.TXT", 3, 1,
	[
		new("NAVIGATION_0", SegmentSource.MarkerBodyLine, Marker: "NAVIGATION", BodyLineIndex: 0),
		new("CONSTRUCTION_0", SegmentSource.MarkerBodyLine, Marker: "CONSTRUCTION", BodyLineIndex: 0),
		new("HORSEBACK_RIDING_0", SegmentSource.MarkerBodyLine, Marker: "HORSEBACK RIDING", BodyLineIndex: 0),
		new("CEREMONIAL_BURIAL_0", SegmentSource.MarkerBodyLine, Marker: "CEREMONIAL BURIAL", BodyLineIndex: 0),
		new("POTTERY_0", SegmentSource.MarkerBodyLine, Marker: "POTTERY", BodyLineIndex: 0)
	]),
	new("BLURB1.TXT", 3, 1,
	[
		new("PALACE_0", SegmentSource.MarkerBodyLine, Marker: "PALACE", BodyLineIndex: 0),
		new("BARRACKS_0", SegmentSource.MarkerBodyLine, Marker: "BARRACKS", BodyLineIndex: 0),
		new("GRANARY_0", SegmentSource.MarkerBodyLine, Marker: "GRANARY", BodyLineIndex: 0),
		new("TEMPLE_0", SegmentSource.MarkerBodyLine, Marker: "TEMPLE", BodyLineIndex: 0),
		new("MARKETPLACE_0", SegmentSource.MarkerBodyLine, Marker: "MARKETPLACE", BodyLineIndex: 0)
	]),
	new("BLURB2.TXT", 3, 1,
	[
		new("PHALANX_0", SegmentSource.MarkerBodyLine, Marker: "PHALANX", BodyLineIndex: 0),
		new("KNIGHTS_0", SegmentSource.MarkerBodyLine, Marker: "KNIGHTS", BodyLineIndex: 0),
		new("TRIREME_0", SegmentSource.MarkerBodyLine, Marker: "TRIREME", BodyLineIndex: 0),
		new("SAIL_0", SegmentSource.MarkerBodyLine, Marker: "SAIL", BodyLineIndex: 0),
		new("FRIGATE_0", SegmentSource.MarkerBodyLine, Marker: "FRIGATE", BodyLineIndex: 0)
	]),
	new("BLURB3.TXT", 3, 1,
	[
		new("BYTE_0000", SegmentSource.ByteWindow, Offset: 0, Length: 48),
		new("BYTE_0064", SegmentSource.ByteWindow, Offset: 64, Length: 48),
		new("BYTE_0128", SegmentSource.ByteWindow, Offset: 128, Length: 48),
		new("BYTE_0192", SegmentSource.ByteWindow, Offset: 192, Length: 48),
		new("BYTE_0256", SegmentSource.ByteWindow, Offset: 256, Length: 48)
	]),
	new("BLURB4.TXT", 3, 1,
	[
		new("ROADS_0", SegmentSource.MarkerBodyLine, Marker: "ROADS", BodyLineIndex: 0),
		new("RAILROADS_0", SegmentSource.MarkerBodyLine, Marker: "RAILROADS", BodyLineIndex: 0),
		new("IRRIGATION_0", SegmentSource.MarkerBodyLine, Marker: "IRRIGATION", BodyLineIndex: 0),
		new("MINING_0", SegmentSource.MarkerBodyLine, Marker: "MINING", BodyLineIndex: 0),
		new("FORTIFY_0", SegmentSource.MarkerBodyLine, Marker: "FORTIFY", BodyLineIndex: 0)
	]),
	new("ERROR.TXT", 3, 1,
	[
		new("ZOC_0", SegmentSource.MarkerBodyLine, Marker: "ZOC", BodyLineIndex: 0),
		new("TRIREME_0", SegmentSource.MarkerBodyLine, Marker: "TRIREME", BodyLineIndex: 0),
		new("FUEL_0", SegmentSource.MarkerBodyLine, Marker: "FUEL", BodyLineIndex: 0),
		new("AMPHIB_0", SegmentSource.MarkerBodyLine, Marker: "AMPHIB", BodyLineIndex: 0),
		new("NOWATER_0", SegmentSource.MarkerBodyLine, Marker: "NOWATER", BodyLineIndex: 0)
	]),
	new("HELP.TXT", 3, 1,
	[
		new("FIRSTMOVE_0", SegmentSource.MarkerBodyLine, Marker: "FIRSTMOVE", BodyLineIndex: 0),
		new("BUILDCITY_0", SegmentSource.MarkerBodyLine, Marker: "BUILDCITY", BodyLineIndex: 0),
		new("FIRSTPRODUCT_0", SegmentSource.MarkerBodyLine, Marker: "FIRSTPRODUCT", BodyLineIndex: 0),
		new("FIRSTCIV_0", SegmentSource.MarkerBodyLine, Marker: "FIRSTCIV", BodyLineIndex: 0),
		new("FIRSTUNIT1_0", SegmentSource.MarkerBodyLine, Marker: "FIRSTUNIT1", BodyLineIndex: 0)
	]),
	new("KING.TXT", 3, 1,
	[
		new("NEWSA_0", SegmentSource.MarkerBodyLine, Marker: "NEWSA", BodyLineIndex: 0),
		new("NEWSC_0", SegmentSource.MarkerBodyLine, Marker: "NEWSC", BodyLineIndex: 0),
		new("NEWSE_0", SegmentSource.MarkerBodyLine, Marker: "NEWSE", BodyLineIndex: 0),
		new("NEWSG_0", SegmentSource.MarkerBodyLine, Marker: "NEWSG", BodyLineIndex: 0),
		new("NEWSI_0", SegmentSource.MarkerBodyLine, Marker: "NEWSI", BodyLineIndex: 0)
	]),
	new("PRODUCE.TXT", 3, 1,
	[
		new("TEMPLE_0", SegmentSource.MarkerBodyLine, Marker: "Temple", BodyLineIndex: 0),
		new("BARRACKS_1", SegmentSource.MarkerBodyLine, Marker: "Barracks", BodyLineIndex: 1),
		new("GRANARY_1", SegmentSource.MarkerBodyLine, Marker: "Granary", BodyLineIndex: 1),
		new("LIBRARY_1", SegmentSource.MarkerBodyLine, Marker: "Library", BodyLineIndex: 1),
		new("CITY_WALLS_1", SegmentSource.MarkerBodyLine, Marker: "City Walls", BodyLineIndex: 1)
	])
];

static void PrintHelp()
{
	Console.WriteLine("civtext-hashgen");
	Console.WriteLine("Generate a replacement C# initializer for OriginalTextLanguageValidationFileDefinition hashes.");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  civtext-hashgen <orig-data-directory> [--trace] [--output <path>]");
	Console.WriteLine();
	Console.WriteLine("Examples:");
	Console.WriteLine("  dotnet run --project civtext-hashgen -- /home/christian/projekte/civ_orig");
	Console.WriteLine("  dotnet run --project civtext-hashgen -- /home/christian/projekte/civ_orig --output src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs");
	Console.WriteLine("  dotnet run --project civtext-hashgen -- /home/christian/projekte/civ_orig --trace");
	Console.WriteLine();
	Console.WriteLine("Notes:");
	Console.WriteLine("  --output writes/overwrites the generated C# file.");
	Console.WriteLine("  --trace writes segment provenance details to stderr.");
}

internal enum SegmentSource
{
	MarkerBodyLine,
	ByteWindow
}

internal sealed record SegmentTemplate(
	string SegmentId,
	SegmentSource Source,
	string? Marker = null,
	int BodyLineIndex = 0,
	int Offset = 0,
	int Length = 0);

internal sealed record FileDefinitionTemplate(
	string FileName,
	int MinimumMatches,
	int MaximumMismatches,
	SegmentTemplate[] Segments);

internal sealed record SegmentReadResult(
	string Hash,
	string RawPreview,
	string NormalizedPreview,
	string FirstTenWords);

internal sealed record GeneratedSegmentDefinition(
	SegmentTemplate Template,
	SegmentReadResult Result);

internal sealed record GeneratedFileDefinition(
	FileDefinitionTemplate Template,
	IReadOnlyList<GeneratedSegmentDefinition> Segments);

internal sealed record GenerationResult(
	string GeneratedCode,
	IReadOnlyList<string> Errors);
