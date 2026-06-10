// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CivOne.Enums;
using CivOne.Services;

namespace CivOne
{
	internal sealed class Program
	{
		private static readonly string[] MacSdlLibraryCandidates =
		[
			"/Library/Frameworks/SDL2.framework/Versions/Current/SDL2",
			"/opt/homebrew/lib/libSDL2.dylib",
			"/opt/homebrew/lib/libSDL2-2.0.0.dylib",
			"/usr/local/lib/libSDL2.dylib",
			"/usr/local/lib/libSDL2-2.0.0.dylib"
		];

		private static string ErrorText => @"civone-sdl: Invalid options: '{0}'
Try 'civone-sdl --help' for more information.
";

		private static void RegisterNativeResolver()
		{
			if (Native.Platform != Platform.macOS)
			{
				return;
			}

			NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, ResolveSdlLibrary);
		}

		private static IntPtr ResolveSdlLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if (!libraryName.Contains("SDL2", StringComparison.OrdinalIgnoreCase))
			{
				return IntPtr.Zero;
			}

			foreach (string libraryPath in MacSdlLibraryCandidates)
			{
				if (NativeLibrary.TryLoad(libraryPath, out IntPtr handle))
				{
					return handle;
				}
			}

			return IntPtr.Zero;
		}

		private static void Main(string[] args)
		{
			RegisterNativeResolver();

			RuntimeSettings settings = new();
			settings["software-render"] = false;
			settings["no-sound"] = false;
			settings[Runtime.DEFAULT_PROFILE_NAME_KEY] = Runtime.DEFAULT_PROFILE_NAME_VALUE;
			settings["mcp-artifacts"] = null;
			settings["mcp-saves"] = null;
			settings.ConsoleLogging = true;
			string? languagePostfix = null;
			for (int i = 0; i < args.Length; i++)
			{
				string cmd = args[i].TrimStart('-');
				if (i == 0 && args.Length == 1)
				{
					switch (cmd)
					{
						case "help":
						case "h":
							Console.WriteLine(Resources.HelpText);
							return;
						case "desktop-icon":
						case "D":
							switch (Native.Platform)
							{
								case Platform.Windows:
									Console.Write("Creating desktop icon... ");
									Console.WriteLine(Native.CreateDesktopIcon("CivOne", "An open source implementation of Sid Meier's Civilization") ? "done" : "failed");
									break;
								default:
									Console.WriteLine($"Creating a desktop icon is not implemented on {Native.Platform.Name()}.");
									break;
							}
							return;
					}
				}

				switch (cmd)
				{
					case "demo": settings.Demo = true; continue;
					case "debug": settings["debug"] = true; continue;
					case "setup": settings.Setup = true; continue;
					case "free": settings.Free = true; continue;
					case "mcp": settings.McpEnabled = true; continue;
					case "mcp-http":
						settings.McpEnabled = true;
						settings["mcp-http"] = true;
						continue;
					case "mcp-http-port":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing port argument for --mcp-http-port");
							return;
						}

						if (!int.TryParse(args[++i], out int httpPort) || httpPort < 1 || httpPort > 65535)
						{
							Console.WriteLine("Invalid value for --mcp-http-port (expected 1..65535)");
							return;
						}

						settings["mcp-http-port"] = httpPort;
						continue;
					case "mcp-http-timeout-ms":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing timeout argument for --mcp-http-timeout-ms");
							return;
						}

						if (!int.TryParse(args[++i], out int httpTimeoutMs) || httpTimeoutMs < 1000)
						{
							Console.WriteLine("Invalid value for --mcp-http-timeout-ms (expected >= 1000)");
							return;
						}

						settings["mcp-http-timeout-ms"] = httpTimeoutMs;
						continue;
					case "mcp-no-auth": settings.McpNoAuth = true; continue;
					case "no-sound": settings["no-sound"] = true; continue;
					case "no-data-check": settings.DataCheck = false; continue;
					case "console-log": settings.ConsoleLogging = true; continue;
					case "no-console-log": settings.ConsoleLogging = false; continue;
					case "mcp-artifacts":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing folder path argument for --mcp-artifacts");
							return;
						}

						var artifactsDir = args[++i];
						settings["mcp-artifacts"] = artifactsDir;
						Console.WriteLine($"MCP artifacts folder set to \"{artifactsDir}\"");
						continue;
					case "mcp-saves":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing folder path argument for --mcp-saves");
							return;
						}

						var savesDir = args[++i];
						settings["mcp-saves"] = savesDir;

						if (!Directory.Exists(savesDir))
						{
							Console.WriteLine($"MCP saves folder \"{savesDir}\" does not exist.");
							return;
						}
						Console.WriteLine($"MCP saves folder set to \"{savesDir}\"");
						continue;
					case "profile":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing profile name argument");
							return;
						}

						settings["profile-name"] = args[++i];
						Console.WriteLine($@"Using profile ""{settings["profile-name"]}""");
						break;
					case "language":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing language postfix argument");
							return;
						}

						languagePostfix = args[++i];
						if (string.IsNullOrEmpty(languagePostfix))
						{
							Console.WriteLine("Invalid language postfix.");
							return;
						}

						break;
					case "load-slot":
						// --load-slot [a-z1..10] (default no options defaults to null)
						// optional argument is a drive letter (a-z) and a slot number (1-10)
						if (args.GetUpperBound(0) == i)
						{
							settings.LoadSaveGameSlot = RuntimeSettings.UseLoadingScreen;
							break;
						}

						// use regex to parse the drive letter and slot number
						string slot = args[++i];
						Regex regex = new(@"^([a-z])([1-9]|10)$", RegexOptions.IgnoreCase);
						Match match = regex.Match(slot);
						if (!match.Success)
						{
							Console.WriteLine("Invalid load slot format. Use: --load-slot [a-z1..10] to specify a drive letter and slot number for the game file.");
							return;
						}

						char driveLetter = char.ToUpper(match.Groups[1].Value[0], CultureInfo.InvariantCulture);
						int slotId = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

						settings.LoadSaveGameSlot = new Tuple<char, int>(driveLetter, slotId);
						break;
					case "load-cos":
						// --load-cos <path/to/file.cos>
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing file path argument for --load-cos");
							return;
						}
						settings.LoadCosFile = args[++i];
						break;
					case "skip-credits": settings.ShowCredits = false; continue;
					case "skip-intro": settings.ShowIntro = false; continue;
					case "software-render": settings["software-render"] = true; continue;
					case "seed":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing seed value argument for --seed");
							return;
						}
						if (!ushort.TryParse(args[++i], out ushort seed))
						{
							Console.WriteLine("Invalid seed value. Use an integer between 0 and 65535.");
							return;
						}
						settings.InitialSeed = seed;
						break;
					default: Console.WriteLine(ErrorText); return;
				}
			}

			if (settings.Free)
			{
				settings["no-sound"] = true;
			}

			bool isLanguageParameterApplied = ApplyLanguageParameter(settings, languagePostfix);
			if (!isLanguageParameterApplied)
			{
				return;
			}

			using Runtime runtime = new(settings);
			IUtcClock clock = new SystemUtcClock();
			IDebounceService debounceService = DebounceServiceFactory.Create(message => runtime.Log(message), clock);

			using GameWindow window = new(runtime, (bool)(settings["software-render"] ?? false), debounceService);
			runtime.Log("Game started");
			window.Run();
			runtime.Log("Game stopped");
		}

		private static bool ApplyLanguageParameter(RuntimeSettings settings, string? languagePostfix)
		{
			if (string.IsNullOrEmpty(languagePostfix))
			{
				return true;
			}
			if (string.Equals(languagePostfix, "identity", StringComparison.OrdinalIgnoreCase))
			{
				settings.LanguagePostfix = "identity";
			}
			else
			{
				string storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CivOne");
				if (!TranslationServiceFactory.TryUseLanguage(storageDirectory, languagePostfix, out string? error, message => Console.WriteLine(message)))
				{
					Console.WriteLine($"Could not activate translation language '{languagePostfix}': {error ?? "unknown error"}");
					return false;
				}

				settings.LanguagePostfix = languagePostfix;
			}

			return true;
		}
	}
}