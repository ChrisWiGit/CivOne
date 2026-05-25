using CivTranslateInteractive;

if (args.Length == 0 || args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)))
{
	PrintHelp();
	return args.Length == 0 ? 1 : 0;
}

if (args.Length != 1)
{
	Console.WriteLine("Error: Exactly one translation file path is required.");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

IInteractiveConsole interactiveConsole = new SystemInteractiveConsole();
ITranslationDocumentRepository translationDocumentRepository = new TranslationDocumentRepository();
IValuesFileRepository valuesFileRepository = new ValuesFileRepository();
TranslationRoundtripWorkflow workflow = new(interactiveConsole, translationDocumentRepository, valuesFileRepository);

string translationFilePath = Path.GetFullPath(args[0]);
return workflow.Run(translationFilePath);

static void PrintHelp()
{
	Console.WriteLine("civtranslate-interactive");
	Console.WriteLine("Create a values-only work file from a key=value translation file,");
	Console.WriteLine("wait for manual translation, and write translated values back.");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  civtranslate-interactive <translation-file>");
	Console.WriteLine();
	Console.WriteLine("Example:");
	Console.WriteLine("  civtranslate-interactive ./translation/civ_german.txt");
}
