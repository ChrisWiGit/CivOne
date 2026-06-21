using System.Collections.ObjectModel;

namespace CivTranslateInteractive;

public sealed class TranslationDocument
{
	public required Collection<ITranslationLine> Lines { get; init; }
	public required Collection<TranslationEntryLine> Entries { get; init; }
}

public interface ITranslationLine;

public sealed record TranslationCommentLine(string Value) : ITranslationLine;

public sealed record TranslationEmptyLine : ITranslationLine;

public sealed record TranslationEntryLine(string Key, string Value) : ITranslationLine
{
	public string Value { get; set; } = Value;
}
