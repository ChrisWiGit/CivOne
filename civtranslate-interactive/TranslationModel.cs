namespace CivTranslateInteractive;

public sealed class TranslationDocument
{
	public required List<ITranslationLine> Lines { get; init; }
	public required List<TranslationEntryLine> Entries { get; init; }
}

public interface ITranslationLine;

public sealed record TranslationCommentLine(string Value) : ITranslationLine;

public sealed record TranslationEmptyLine : ITranslationLine;

public sealed record TranslationEntryLine(string Key, string Value) : ITranslationLine
{
	public string Value { get; set; } = Value;
}
