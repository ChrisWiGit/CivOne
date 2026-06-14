using CivTranslateInteractive;

if (args.Length == 0 || HasHelpSwitch(args))
{
	PrintHelp();
	return args.Length == 0 ? 1 : 0;
}

if (!TryParseArguments(args, out string languagePostfix, out string parseError))
{
	Console.WriteLine($"Error: {parseError}");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

string trimmedPostfix = languagePostfix.Trim();
if (trimmedPostfix.Length == 0)
{
	Console.WriteLine("Error: --language value must not be empty.");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

if (trimmedPostfix.Contains('/') || trimmedPostfix.Contains('\\'))
{
	Console.WriteLine("Error: --language must be a postfix only, not a path.");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

string translationFileName = $"civ_{trimmedPostfix}.txt";
string translationFilePath = Path.GetFullPath(Path.Combine("translation", translationFileName));
if (!File.Exists(translationFilePath))
{
	Console.WriteLine($"Error: Language file not found: {translationFilePath}");
	Console.WriteLine("Expected file pattern: translation/civ_<postfix>.txt");
	return 2;
}

IInteractiveConsole interactiveConsole = new SystemInteractiveConsole();
ITranslationDocumentRepository translationDocumentRepository = new TranslationDocumentRepository();
IValuesFileRepository valuesFileRepository = new ValuesFileRepository();
TranslationRoundtripWorkflow workflow = new(interactiveConsole, translationDocumentRepository, valuesFileRepository);

return workflow.Run(translationFilePath);

static bool HasHelpSwitch(string[] args) => args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

static bool TryParseArguments(string[] args, out string languagePostfix, out string error)
{
	languagePostfix = string.Empty;
	error = string.Empty;

	for (int i = 0; i < args.Length; i++)
	{
		string arg = args[i];
		if (arg.Equals("--language", StringComparison.OrdinalIgnoreCase) || arg.Equals("-l", StringComparison.OrdinalIgnoreCase))
		{
			if (i + 1 >= args.Length)
			{
				error = "--language requires a postfix value.";
				return false;
			}

			if (languagePostfix.Length != 0)
			{
				error = "--language may only be provided once.";
				return false;
			}

			languagePostfix = args[++i];
			continue;
		}

		if (arg.StartsWith('-'))
		{
			error = $"Unknown option: {arg}";
			return false;
		}

		error = "Positional arguments are not supported. Use --language <postfix>.";
		return false;
	}

	if (languagePostfix.Length == 0)
	{
		error = "Missing required option: --language <postfix>.";
		return false;
	}

	return true;
}

static void PrintHelp()
{
	Console.WriteLine("civtranslate-interactive");
	Console.WriteLine("Create a values-only work file from a key=value translation file,");
	Console.WriteLine("wait for manual translation, and write translated values back.");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  civtranslate-interactive --language <postfix>");
	Console.WriteLine();
	Console.WriteLine("Example:");
	Console.WriteLine("  civtranslate-interactive --language german");
}
