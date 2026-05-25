using System.Text;

namespace CivTranslateMergeKeys;

internal interface IKeyValueMergeWorkflow
{
	int Run(string sourceFilePath, string targetFilePath);
}

internal sealed class KeyValueMergeWorkflow : IKeyValueMergeWorkflow
{
	private const string EqualsPlaceholder = "[EQ]";

	public int Run(string sourceFilePath, string targetFilePath)
	{
		string sourcePath = Path.GetFullPath(sourceFilePath);
		string targetPath = Path.GetFullPath(targetFilePath);

		if (!File.Exists(sourcePath))
		{
			Console.Error.WriteLine($"Error: Source file not found: {sourcePath}");
			return 2;
		}

		List<KeyValueEntry> sourceEntries = ReadEntries(sourcePath);
		List<KeyValueEntry> targetEntries = File.Exists(targetPath) ? ReadEntries(targetPath) : [];
		HashSet<string> targetKeys = targetEntries.Select(entry => entry.NormalizedKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
		List<KeyValueEntry> missingEntries = [];
		HashSet<string> missingKeys = [];

		foreach (KeyValueEntry entry in sourceEntries)
		{
			if (targetKeys.Contains(entry.NormalizedKey))
			{
				continue;
			}

			if (!missingKeys.Add(entry.NormalizedKey))
			{
				continue;
			}

			missingEntries.Add(entry);
		}

		if (missingEntries.Count > 0)
		{
			AppendMissingEntries(targetPath, missingEntries);
		}

		Console.WriteLine("Merge complete.");
		Console.WriteLine($"Source keys: {sourceEntries.Count}");
		Console.WriteLine($"Target keys before merge: {targetEntries.Count}");
		Console.WriteLine($"Keys added: {missingEntries.Count}");
		Console.WriteLine($"Target written: {targetPath}");
		return 0;
	}

	private static List<KeyValueEntry> ReadEntries(string filePath)
	{
		List<KeyValueEntry> entries = [];
		HashSet<string> seenKeys = [];
		string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}

			int separatorIndex = line.IndexOf('=');
			if (separatorIndex < 0)
			{
				Console.WriteLine($"Warning: Ignoring malformed line ({filePath}:{i + 1}): {line}");
				continue;
			}

			string key = NormalizeKey(UnescapeEquals(line[..separatorIndex]).Trim());
			if (key.Length == 0)
			{
				continue;
			}

			if (!seenKeys.Add(key))
			{
				continue;
			}

			string value = UnescapeEquals(line[(separatorIndex + 1)..]);
			entries.Add(new KeyValueEntry(key, value));
		}

		return entries;
	}

	private static void AppendMissingEntries(string targetPath, IReadOnlyList<KeyValueEntry> missingEntries)
	{
		string? targetDirectory = Path.GetDirectoryName(targetPath);
		if (!string.IsNullOrWhiteSpace(targetDirectory))
		{
			Directory.CreateDirectory(targetDirectory);
		}

		string existingContent = File.Exists(targetPath)
			? File.ReadAllText(targetPath, Encoding.UTF8)
			: string.Empty;

		StringBuilder builder = new(existingContent);
		if (existingContent.Length > 0 && !EndsWithLineBreak(existingContent))
		{
			builder.AppendLine();
		}

		foreach (KeyValueEntry entry in missingEntries)
		{
			builder.AppendLine($"{EscapeEquals(entry.NormalizedKey)}={EscapeEquals(entry.Value)}");
		}

		File.WriteAllText(targetPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	private static bool EndsWithLineBreak(string value) => value.EndsWith('\n') || value.EndsWith('\r');

	private static string NormalizeKey(string key) => key.ToUpperInvariant();

	private static string EscapeEquals(string value) => value.Replace("=", EqualsPlaceholder, StringComparison.Ordinal);

	private static string UnescapeEquals(string value) => value.Replace(EqualsPlaceholder, "=", StringComparison.Ordinal);
}

internal readonly record struct KeyValueEntry(string NormalizedKey, string Value);
