using System.Text;

const string DefaultOutputFileName = "translate_all.txt";
const string DefaultObsoleteKeysFileName = "obsoletekeys.txt";
const string EqualsPlaceholder = "[EQ]";

static List<string> GetDefaultHeader() =>
[
	"# CivOne - General translation file",
	"# This file is used to list all translation keys and their default English values.",
	"# See README.md from civtranslate for instructions on how to create a translation file for a specific language."
];

if (args.Length == 0)
{
	PrintHelp();
	return 1;
}

if (HasHelpSwitch(args))
{
	PrintHelp();
	return 0;
}

if (!TryParseArguments(args, out string inputFolderArgument, out string outputPath, out string parseError))
{
	Console.Error.WriteLine($"Error: {parseError}");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

string inputFolder = Path.GetFullPath(inputFolderArgument);
if (!Directory.Exists(inputFolder))
{
	Console.Error.WriteLine($"Error: Input directory not found: {inputFolder}");
	return 2;
}

if (string.IsNullOrWhiteSpace(outputPath))
{
	outputPath = DefaultOutputFileName;
}

string outputFile = Path.GetFullPath(outputPath);
string outputDirectory = Path.GetDirectoryName(outputFile) ?? Directory.GetCurrentDirectory();
string obsoleteKeysFile = Path.Combine(outputDirectory, DefaultObsoleteKeysFileName);

ScanReport scanReport = ScanDirectory(inputFolder);
TranslationFile existingFile = ReadTranslationFileIfExists(outputFile);
MergeReport mergeReport = Merge(scanReport, existingFile);
WriteTranslationFile(outputFile, mergeReport);
WriteObsoleteKeysFile(obsoleteKeysFile, mergeReport.ObsoleteEntries);

PrintWarnings(scanReport, mergeReport);
PrintSummary(scanReport, existingFile, mergeReport, outputFile, obsoleteKeysFile);

return 0;

static bool HasHelpSwitch(string[] args) => args.Any(x => x.Equals("--help", StringComparison.OrdinalIgnoreCase) || x.Equals("-h", StringComparison.OrdinalIgnoreCase));

static bool TryParseArguments(string[] args, out string inputFolder, out string outputFile, out string error)
{
	inputFolder = string.Empty;
	outputFile = DefaultOutputFileName;
	error = string.Empty;

	List<string> positionals = [];

	for (int i = 0; i < args.Length; i++)
	{
		string arg = args[i];
		if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase))
		{
			if (i + 1 >= args.Length)
			{
				error = "--output requires a filename.";
				return false;
			}

			outputFile = args[++i];
			if (string.IsNullOrWhiteSpace(outputFile))
			{
				error = "--output value must not be empty.";
				return false;
			}

			continue;
		}

		if (arg.StartsWith("--", StringComparison.Ordinal) && !arg.Equals("--output", StringComparison.OrdinalIgnoreCase))
		{
			error = $"Unknown option: {arg}";
			return false;
		}

		if (arg.StartsWith("-", StringComparison.Ordinal) && !arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
		{
			error = $"Unknown option: {arg}";
			return false;
		}

		positionals.Add(arg);
	}

	if (positionals.Count != 1)
	{
		error = "Exactly one input directory is required.";
		return false;
	}

	inputFolder = positionals[0];
	return true;
}

static ScanReport ScanDirectory(string inputFolder)
{
	List<FoundKey> foundKeys = [];
	Dictionary<string, FoundKey> uniqueByNormalized = new(StringComparer.OrdinalIgnoreCase);
	List<InterpolationWarning> interpolationWarnings = [];

	string[] files = Directory
		.EnumerateFiles(inputFolder, "*.cs", SearchOption.AllDirectories)
		.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
		.ToArray();

	foreach (string file in files)
	{
		string content = File.ReadAllText(file, Encoding.UTF8);

		foreach (InvocationCandidate candidate in EnumerateInvocationCandidates(content))
		{
			if (!TryExtractFirstStringArgument(content, candidate.OpenParenIndex, out ExtractedArgument extracted))
			{
				continue;
			}

			if (extracted.IsInterpolated)
			{
				interpolationWarnings.Add(new InterpolationWarning(file, extracted.Line, candidate.PatternName));
				continue;
			}

			string normalizedKey = NormalizeKey(extracted.Text);
			if (normalizedKey.Length == 0)
			{
				continue;
			}

			if (!uniqueByNormalized.ContainsKey(normalizedKey))
			{
				FoundKey found = new(normalizedKey, extracted.Text, file, extracted.Line);
				uniqueByNormalized[normalizedKey] = found;
				foundKeys.Add(found);
			}
		}
	}

	return new ScanReport(files.Length, foundKeys, interpolationWarnings);
}

static IEnumerable<InvocationCandidate> EnumerateInvocationCandidates(string content)
{
	for (int index = 0; index < content.Length; index++)
	{
		if (TryMatchInvocation(content, index, "TranslateFormattedArray", out int openParen0))
		{
			yield return new InvocationCandidate("TranslateFormattedArray", openParen0);
			index = openParen0;
			continue;
		}

		if (TryMatchInvocation(content, index, "TranslateFormatted", out int openParen1))
		{
			yield return new InvocationCandidate("TranslateFormatted", openParen1);
			index = openParen1;
			continue;
		}

		if (TryMatchInvocation(content, index, "TranslateArray", out int openParen12))
		{
			yield return new InvocationCandidate("TranslateArray", openParen12);
			index = openParen12;
			continue;
		}

		if (TryMatchInvocation(content, index, "TF", out int openParen13))
		{
			yield return new InvocationCandidate("TF", openParen13);
			index = openParen13;
			continue;
		}

		if (TryMatchInvocation(content, index, ".Translate", out int openParen2))
		{
			yield return new InvocationCandidate(".Translate", openParen2);
			index = openParen2;
			continue;
		}
		if (TryMatchInvocation(content, index, "Translate", out int openParen3))
		{
			yield return new InvocationCandidate("Translate", openParen3);
			index = openParen3;
			continue;
		}

		if (TryMatchTInvocation(content, index, out int openParen4))
		{
			yield return new InvocationCandidate("T", openParen4);
			index = openParen4;
		}
	}
}

static bool TryMatchInvocation(string content, int index, string methodName, out int openParenIndex)
{
	openParenIndex = -1;

	if (!content.AsSpan(index).StartsWith(methodName, StringComparison.Ordinal))
	{
		return false;
	}

	int cursor = index + methodName.Length;
	while (cursor < content.Length && char.IsWhiteSpace(content[cursor]))
	{
		cursor++;
	}

	if (cursor >= content.Length || content[cursor] != '(')
	{
		return false;
	}

	openParenIndex = cursor;
	return true;
}

static bool TryMatchTInvocation(string content, int index, out int openParenIndex)
{
	openParenIndex = -1;

	if (content[index] != 'T')
	{
		return false;
	}

	if (index > 0)
	{
		char prev = content[index - 1];
		if (char.IsLetterOrDigit(prev) || prev == '_')
		{
			return false;
		}
	}

	int cursor = index + 1;
	while (cursor < content.Length && char.IsWhiteSpace(content[cursor]))
	{
		cursor++;
	}

	if (cursor >= content.Length || content[cursor] != '(')
	{
		return false;
	}

	openParenIndex = cursor;
	return true;
}

static bool TryExtractFirstStringArgument(string content, int openParenIndex, out ExtractedArgument argument)
{
	argument = default;

	int cursor = openParenIndex + 1;
	while (cursor < content.Length && char.IsWhiteSpace(content[cursor]))
	{
		cursor++;
	}

	if (!TryParseConcatenatedStringExpression(content, cursor, out string text, out bool isInterpolated, out int firstQuoteIndex))
	{
		return false;
	}

	int line = GetLineNumber(content, firstQuoteIndex);

	text = EscapeControlCharacters(text);

	argument = new ExtractedArgument(text, isInterpolated, line);
	return true;
}

static bool TryParseConcatenatedStringExpression(string content, int startIndex, out string text, out bool isInterpolated, out int firstQuoteIndex)
{
	text = string.Empty;
	isInterpolated = false;
	firstQuoteIndex = -1;

	StringBuilder builder = new();
	int cursor = startIndex;
	bool parsedAny = false;

	while (cursor < content.Length)
	{
		while (cursor < content.Length && char.IsWhiteSpace(content[cursor]))
		{
			cursor++;
		}

		bool hasDollar = false;
		bool isVerbatim = false;

		bool consumedPrefix = true;
		while (consumedPrefix && cursor < content.Length)
		{
			consumedPrefix = false;
			if (content[cursor] == '$')
			{
				hasDollar = true;
				cursor++;
				consumedPrefix = true;
			}
			else if (content[cursor] == '@')
			{
				isVerbatim = true;
				cursor++;
				consumedPrefix = true;
			}
		}

		if (cursor >= content.Length || content[cursor] != '"')
		{
			return false;
		}

		if (firstQuoteIndex < 0)
		{
			firstQuoteIndex = cursor;
		}

		bool parsed = isVerbatim
			? TryParseVerbatimString(content, cursor, out string segment, out int nextIndex)
			: TryParseRegularString(content, cursor, out segment, out nextIndex);

		if (!parsed)
		{
			return false;
		}

		parsedAny = true;
		builder.Append(segment);
		isInterpolated |= hasDollar;
		cursor = nextIndex;

		while (cursor < content.Length && char.IsWhiteSpace(content[cursor]))
		{
			cursor++;
		}

		if (cursor < content.Length && content[cursor] == '+')
		{
			cursor++;
			continue;
		}

		break;
	}

	if (!parsedAny)
	{
		return false;
	}

	text = builder.ToString();
	return true;
}

static bool TryParseRegularString(string content, int startQuoteIndex, out string text, out int nextIndex)
{
	text = string.Empty;
	nextIndex = -1;
	StringBuilder builder = new();

	int cursor = startQuoteIndex + 1;
	while (cursor < content.Length)
	{
		char ch = content[cursor];
		if (ch == '"')
		{
			text = builder.ToString();
			nextIndex = cursor + 1;
			return true;
		}

		if (ch == '\\')
		{
			if (cursor + 1 >= content.Length)
			{
				return false;
			}

			char escaped = content[cursor + 1];
			builder.Append(escaped switch
			{
				'n' => '\n',
				'r' => '\r',
				't' => '\t',
				'"' => '"',
				'\\' => '\\',
				_ => escaped
			});
			cursor += 2;
			continue;
		}

		builder.Append(ch);
		cursor++;
	}

	return false;
}

static bool TryParseVerbatimString(string content, int startQuoteIndex, out string text, out int nextIndex)
{
	text = string.Empty;
	nextIndex = -1;
	StringBuilder builder = new();

	int cursor = startQuoteIndex + 1;
	while (cursor < content.Length)
	{
		char ch = content[cursor];
		if (ch == '"')
		{
			if (cursor + 1 < content.Length && content[cursor + 1] == '"')
			{
				builder.Append('"');
				cursor += 2;
				continue;
			}

			text = builder.ToString();
			nextIndex = cursor + 1;
			return true;
		}

		builder.Append(ch);
		cursor++;
	}

	return false;
}

static int GetLineNumber(string content, int position)
{
	int line = 1;
	for (int i = 0; i < position && i < content.Length; i++)
	{
		if (content[i] == '\n')
		{
			line++;
		}
	}

	return line;
}

static TranslationFile ReadTranslationFileIfExists(string outputFile)
{
	if (!File.Exists(outputFile))
	{
		return new TranslationFile([], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), []);
	}

	List<TranslationEntry> entries = [];
	Dictionary<string, string> byNormalized = new(StringComparer.OrdinalIgnoreCase);
	List<string> comments = [];

	string[] lines = File.ReadAllLines(outputFile, Encoding.UTF8);
	for (int i = 0; i < lines.Length; i++)
	{
		string line = lines[i];
		if (string.IsNullOrWhiteSpace(line))
		{
			continue;
		}

		if (line.StartsWith("#"))
		{
			comments.Add(line);
			continue;
		}

		int separatorIndex = line.IndexOf('=');
		if (separatorIndex < 0)
		{
			Console.WriteLine($"Warning: Ignoring malformed line in existing file ({outputFile}:{i + 1}): {line}");
			continue;
		}

		string rawKey = line[..separatorIndex];
		string rawValue = line[(separatorIndex + 1)..];

		string key = UnescapeEquals(rawKey).Trim();
		string value = UnescapeEquals(rawValue);

		if (key.Length == 0)
		{
			continue;
		}

		string normalized = NormalizeKey(key);
		byNormalized[normalized] = value;
		entries.Add(new TranslationEntry(normalized, value));
	}

	return new TranslationFile(entries, byNormalized, comments);
}

static MergeReport Merge(ScanReport scanReport, TranslationFile existingFile)
{
	List<TranslationEntry> finalEntries = [];
	List<TranslationEntry> newEntries = [];
	HashSet<string> usedKeys = new(StringComparer.OrdinalIgnoreCase);
	List<OverwrittenEntry> overwrittenEntries = [];
	int reused = 0;
	int added = 0;
	List<string> comments = existingFile.Comments.Count > 0 ? existingFile.Comments : GetDefaultHeader();

	foreach (FoundKey found in scanReport.FoundKeys)
	{
		if (usedKeys.Contains(found.NormalizedKey))
		{
			continue;
		}

		if (existingFile.ByNormalizedKey.TryGetValue(found.NormalizedKey, out string? existingValue))
		{
			string newValue = found.OriginalText;
			if (!string.Equals(existingValue, newValue, StringComparison.Ordinal))
			{
				overwrittenEntries.Add(new OverwrittenEntry(found.NormalizedKey, existingValue, newValue));
			}
			finalEntries.Add(new TranslationEntry(found.NormalizedKey, newValue));
			reused++;
		}
		else
		{
			newEntries.Add(new TranslationEntry(found.NormalizedKey, found.OriginalText));
			added++;
		}

		usedKeys.Add(found.NormalizedKey);
	}

	List<TranslationEntry> obsoleteEntries = [];
	foreach (TranslationEntry existing in existingFile.Entries)
	{
		if (usedKeys.Contains(existing.NormalizedKey))
		{
			continue;
		}

		obsoleteEntries.Add(existing);
	}

	finalEntries.AddRange(newEntries);
	finalEntries.AddRange(obsoleteEntries);

	return new MergeReport(finalEntries, added, reused, obsoleteEntries, overwrittenEntries, comments);
}

static void WriteTranslationFile(string outputFile, MergeReport mergeReport)
{
	string? directory = Path.GetDirectoryName(outputFile);
	if (!string.IsNullOrWhiteSpace(directory))
	{
		Directory.CreateDirectory(directory);
	}

	string tempFile = outputFile + ".tmp";

	using (StreamWriter writer = new(tempFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
	{
		foreach (string comment in mergeReport.Comments)
		{
			writer.WriteLine(comment);
		}

		if (mergeReport.Comments.Count > 0)
		{
			writer.WriteLine();
		}

		foreach (TranslationEntry entry in mergeReport.FinalEntries)
		{
			string key = EscapeEquals(entry.NormalizedKey);
			string value = EscapeEquals(entry.Value);
			writer.WriteLine($"{key}={value}");
		}
	}

	if (File.Exists(outputFile))
	{
		File.Delete(outputFile);
	}

	File.Move(tempFile, outputFile);
}

static void WriteObsoleteKeysFile(string obsoleteKeysFile, List<TranslationEntry> obsoleteEntries)
{
	string? directory = Path.GetDirectoryName(obsoleteKeysFile);
	if (!string.IsNullOrWhiteSpace(directory))
	{
		Directory.CreateDirectory(directory);
	}

	string tempFile = obsoleteKeysFile + ".tmp";

	using (StreamWriter writer = new(tempFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
	{
		foreach (TranslationEntry entry in obsoleteEntries)
		{
			string key = EscapeEquals(entry.NormalizedKey);
			string value = EscapeEquals(entry.Value);
			writer.WriteLine($"{key}={value}");
		}
	}

	if (File.Exists(obsoleteKeysFile))
	{
		File.Delete(obsoleteKeysFile);
	}

	File.Move(tempFile, obsoleteKeysFile);
}

static void PrintWarnings(ScanReport scanReport, MergeReport mergeReport)
{
	foreach (InterpolationWarning warning in scanReport.InterpolationWarnings)
	{
		Console.WriteLine($"Warning: Interpolated string ignored ({warning.Pattern}) at {warning.FilePath}:{warning.Line}");
	}

	foreach (OverwrittenEntry ow in mergeReport.OverwrittenEntries)
	{
		Console.WriteLine($"Info: Key overwritten: {ow.NormalizedKey}");
		Console.WriteLine($"  old: {ow.OldValue}");
		Console.WriteLine($"  new: {ow.NewValue}");
	}

	foreach (TranslationEntry obsolete in mergeReport.ObsoleteEntries)
	{
		Console.WriteLine($"Warning: Key not found in current scan but kept in output: {obsolete.NormalizedKey}");
	}
}

static void PrintSummary(ScanReport scanReport, TranslationFile existingFile, MergeReport mergeReport, string outputFile, string obsoleteKeysFile)
{
	Console.WriteLine();
	Console.WriteLine("Scan complete.");
	Console.WriteLine($"Files scanned: {scanReport.FileCount}");
	Console.WriteLine($"Keys found: {scanReport.FoundKeys.Count}");
	Console.WriteLine($"Interpolated keys ignored: {scanReport.InterpolationWarnings.Count}");
	Console.WriteLine($"Existing file entries loaded: {existingFile.Entries.Count}");
	Console.WriteLine($"Keys reused: {mergeReport.ReusedCount}");
	Console.WriteLine($"Keys added: {mergeReport.AddedCount}");
	Console.WriteLine($"Keys overwritten: {mergeReport.OverwrittenEntries.Count}");
	Console.WriteLine($"Obsolete keys kept: {mergeReport.ObsoleteEntries.Count}");
	Console.WriteLine($"Output written: {outputFile}");
	Console.WriteLine($"Obsolete keys file: {obsoleteKeysFile}");
}

static string NormalizeKey(string key) => key.Trim().ToUpperInvariant();

static string EscapeEquals(string value) => value.Replace("=", EqualsPlaceholder, StringComparison.Ordinal);

static string UnescapeEquals(string value) => value.Replace(EqualsPlaceholder, "=", StringComparison.Ordinal);

static string EscapeControlCharacters(string value) => value
	.Replace("\r\n", "\\n", StringComparison.Ordinal)
	.Replace("\n", "\\n", StringComparison.Ordinal)
	.Replace("\r", "\\r", StringComparison.Ordinal)
	.Replace("\t", "\\t", StringComparison.Ordinal);

static void PrintHelp()
{
	Console.WriteLine("civtranslate");
	Console.WriteLine("Scan C# source files for translation calls and create or update a key=value translation file.");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  civtranslate <input-folder> [--output <file>]");
	Console.WriteLine();
	Console.WriteLine("Options:");
	Console.WriteLine("  --output <file>  Output file path. Default: translate_all.txt");
	Console.WriteLine("  -h, --help       Show this help text");
	Console.WriteLine();
	Console.WriteLine("Scan patterns:");
	Console.WriteLine("  .Translate(\"...\")");
	Console.WriteLine("  Translate(\"...\")");
	Console.WriteLine("  .TranslateFormatted(\"...\", ...)");
	Console.WriteLine("  TF(\"...\", ...)");
	Console.WriteLine("  T(\"...\")");
	Console.WriteLine();
	Console.WriteLine($"Escaping:");
	Console.WriteLine($"  Separator is '='.");
	Console.WriteLine($"  Any '=' in keys or values is replaced with '{EqualsPlaceholder}'.");
	Console.WriteLine();
	Console.WriteLine("Behavior:");
	Console.WriteLine("  Keys are normalized to uppercase before lookup and output.");
	Console.WriteLine("  New keys are written with value equal to key.");
	Console.WriteLine("  Control characters are written as escaped text (e.g. \\n, \\r, \\t).");
	Console.WriteLine("  Interpolated strings ($\"...\") are ignored with warnings.");
	Console.WriteLine("  Existing keys not found in the current scan are kept and warned.");
}

internal readonly record struct InvocationCandidate(string PatternName, int OpenParenIndex);
internal readonly record struct ExtractedArgument(string Text, bool IsInterpolated, int Line);
internal readonly record struct FoundKey(string NormalizedKey, string OriginalText, string FilePath, int Line);
internal readonly record struct InterpolationWarning(string FilePath, int Line, string Pattern);
internal readonly record struct TranslationEntry(string NormalizedKey, string Value);
internal readonly record struct ScanReport(int FileCount, List<FoundKey> FoundKeys, List<InterpolationWarning> InterpolationWarnings);
internal readonly record struct TranslationFile(List<TranslationEntry> Entries, Dictionary<string, string> ByNormalizedKey, List<string> Comments);
internal readonly record struct OverwrittenEntry(string NormalizedKey, string OldValue, string NewValue);
internal readonly record struct MergeReport(List<TranslationEntry> FinalEntries, int AddedCount, int ReusedCount, List<TranslationEntry> ObsoleteEntries, List<OverwrittenEntry> OverwrittenEntries, List<string> Comments);
