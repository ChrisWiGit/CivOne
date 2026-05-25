namespace CivTranslateInteractive;

public sealed class TranslationRoundtripWorkflow(
	IInteractiveConsole interactiveConsole,
	ITranslationDocumentRepository translationDocumentRepository,
	IValuesFileRepository valuesFileRepository)
{
	private readonly IInteractiveConsole _interactiveConsole = interactiveConsole;
	private readonly ITranslationDocumentRepository _translationDocumentRepository = translationDocumentRepository;
	private readonly IValuesFileRepository _valuesFileRepository = valuesFileRepository;

	public int Run(string translationFilePath)
	{
		try
		{
			TranslationDocument document = _translationDocumentRepository.Read(translationFilePath);
			string valuesFilePath = BuildValuesFilePath(translationFilePath);
			if (File.Exists(valuesFilePath))
			{
				_interactiveConsole.WriteLine($"Error: Values work file already exists: {valuesFilePath}");
				_interactiveConsole.WriteLine("Delete or rename the file, then run again.");
				return 2;
			}

			List<string> values = [.. document.Entries.Select(entry => entry.Value)];
			_valuesFileRepository.Write(valuesFilePath, values);

			_interactiveConsole.WriteLine($"Translation file loaded: {translationFilePath}");
			_interactiveConsole.WriteLine($"Values work file created: {valuesFilePath}");
			_interactiveConsole.WriteLine($"Entry count: {document.Entries.Count}");
			_interactiveConsole.WriteLine(string.Empty);
			_interactiveConsole.WriteLine("You can now translate the values work file.");
			_interactiveConsole.WriteLine("Save the file when finished, then press Enter to continue.");
			_interactiveConsole.ReadLine();

			IReadOnlyList<string> translatedValues = _valuesFileRepository.Read(valuesFilePath);
			if (translatedValues.Count != document.Entries.Count)
			{
				_interactiveConsole.WriteLine($"Error: Value count mismatch. Expected {document.Entries.Count}, found {translatedValues.Count}.");
				_interactiveConsole.WriteLine("No changes written to the translation file.");
				return 3;
			}

			for (int i = 0; i < document.Entries.Count; i++)
			{
				document.Entries[i].Value = translatedValues[i];
			}

			_translationDocumentRepository.Write(translationFilePath, document);
			_interactiveConsole.WriteLine("Success: Translated values were written back to the translation file.");
			return 0;
		}
		catch (Exception exception)
		{
			_interactiveConsole.WriteLine($"Error: {exception.Message}");
			return 1;
		}
	}

	public static string BuildValuesFilePath(string translationFilePath)
	{
		string directory = Path.GetDirectoryName(translationFilePath) ?? Directory.GetCurrentDirectory();
		string extension = Path.GetExtension(translationFilePath);
		string nameWithoutExtension = Path.GetFileNameWithoutExtension(translationFilePath);
		string valuesFileName = $"{nameWithoutExtension}.values{extension}";
		return Path.Combine(directory, valuesFileName);
	}
}
