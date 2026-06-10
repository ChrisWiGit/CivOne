using System.Text;

namespace CivTranslateInteractive;

public interface ITranslationDocumentRepository
{
	TranslationDocument Read(string filePath);
	void Write(string filePath, TranslationDocument document);
}

public sealed class TranslationDocumentRepository : ITranslationDocumentRepository
{
	private const string EqualsPlaceholder = "[EQ]";

	public TranslationDocument Read(string filePath)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException("Translation file not found.", filePath);
		}

		string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
		List<ITranslationLine> parsedLines = [];
		List<TranslationEntryLine> entries = [];

		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			if (string.IsNullOrWhiteSpace(line))
			{
				parsedLines.Add(new TranslationEmptyLine());
				continue;
			}

			if (line.StartsWith('#'))
			{
				parsedLines.Add(new TranslationCommentLine(line));
				continue;
			}

			int separatorIndex = line.IndexOf('=');
			if (separatorIndex < 0)
			{
				throw new FormatException($"Malformed translation line at {filePath}:{i + 1}: {line}");
			}

			string key = line[..separatorIndex];
			string value = UnescapeEquals(line[(separatorIndex + 1)..]);
			TranslationEntryLine entry = new(key, value);
			parsedLines.Add(entry);
			entries.Add(entry);
		}

		return new TranslationDocument
		{
			Lines = parsedLines,
			Entries = entries
		};
	}

	public void Write(string filePath, TranslationDocument document)
	{
		string? directoryPath = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		using StreamWriter writer = new(filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		foreach (ITranslationLine line in document.Lines)
		{
			switch (line)
			{
				case TranslationCommentLine commentLine:
					writer.WriteLine(commentLine.Value);
					break;
				case TranslationEntryLine entryLine:
					writer.WriteLine($"{entryLine.Key}={EscapeEquals(entryLine.Value)}");
					break;
				default:
					writer.WriteLine();
					break;
			}
		}
	}

	private static string EscapeEquals(string value) => value.Replace("=", EqualsPlaceholder, StringComparison.Ordinal);
	private static string UnescapeEquals(string value) => value.Replace(EqualsPlaceholder, "=", StringComparison.Ordinal);
}
