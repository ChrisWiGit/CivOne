using CivTranslateMergeKeys;

if (args.Length == 0 || HasHelpSwitch(args))
{
	PrintHelp();
	return args.Length == 0 ? 1 : 0;
}

if (!TryParseArguments(args, out string sourceFilePath, out string targetFilePath, out string error))
{
	Console.Error.WriteLine($"Error: {error}");
	Console.WriteLine();
	PrintHelp();
	return 2;
}

IKeyValueMergeWorkflow workflow = new KeyValueMergeWorkflow();
return workflow.Run(sourceFilePath, targetFilePath);

static bool HasHelpSwitch(string[] args) => args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

static bool TryParseArguments(string[] args, out string sourceFilePath, out string targetFilePath, out string error)
{
	sourceFilePath = string.Empty;
	targetFilePath = string.Empty;
	error = string.Empty;

	if (args.Length != 2)
	{
		error = "Exactly two file paths are required.";
		return false;
	}

	sourceFilePath = args[0];
	targetFilePath = args[1];
	return true;
}

static void PrintHelp()
{
	Console.WriteLine("civtranslate-mergekeys");
	Console.WriteLine("Compare two key=value files and append keys from the first file that are missing in the second file.");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  civtranslate-mergekeys <source-file> <target-file>");
	Console.WriteLine();
	Console.WriteLine("Behavior:");
	Console.WriteLine("  Keys are compared case-insensitively.");
	Console.WriteLine("  Missing keys are appended to the target file in source order.");
	Console.WriteLine("  Existing target entries are preserved.");
	Console.WriteLine("  The target file is created when it does not exist.");
	Console.WriteLine();
	Console.WriteLine("Example:");
	Console.WriteLine("  civtranslate-mergekeys ./translation/all.txt ./translation/civ_german.txt");
}
