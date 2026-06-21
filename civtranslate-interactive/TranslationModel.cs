using System.Collections.ObjectModel;

namespace CivTranslateInteractive;

public sealed class TranslationDocument
{
	public required Collection<TranslationLine> Lines { get; init; }
	public required Collection<TranslationEntryLine> Entries { get; init; }
}

public abstract record TranslationLine;

public sealed record TranslationCommentLine(string Value) : TranslationLine;

public sealed record TranslationEmptyLine : TranslationLine;

public sealed record TranslationEntryLine(string Key, string Value) : TranslationLine
{
	public string Value { get; set; } = Value;
}
