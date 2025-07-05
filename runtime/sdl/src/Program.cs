// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Text.RegularExpressions;
using CivOne.Enums;

namespace CivOne
{
	internal class Program
	{
		private static string ErrorText => @"civone-sdl: Invalid options: '{0}'
Try 'civone-sdl --help' for more information.
";

		private static void Main(string[] args)
		{
			RuntimeSettings settings = new RuntimeSettings();
			settings["software-render"] = false;
			settings["no-sound"] = false;
			settings["profile-name"] = "default";
			for (int i = 0; i < args.Length; i++)
			{
				string cmd = args[i].TrimStart('-');
				if (i == 0 && args.Length == 1)
				{
					switch(cmd)
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

				switch(cmd)
				{
					case "demo": settings.Demo = true; continue;
					case "setup": settings.Setup = true; continue;
					case "free": settings.Free = true; continue;
					case "no-sound": settings["no-sound"] = true; continue;
					case "no-data-check": settings.DataCheck = false; continue;
					case "profile":
						if (args.GetUpperBound(0) == i)
						{
							Console.WriteLine("Missing profile name argument");
							return;
						}
						
						settings["profile-name"] = args[++i];
						Console.WriteLine($@"Using profile ""{settings["profile-name"]}""");
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
						Regex regex = new Regex(@"^([a-z])([0-9]|1[0-5])$", RegexOptions.IgnoreCase);
						Match match = regex.Match(slot);
						if (!match.Success)
						{
							Console.WriteLine("Invalid load slot format. Use: --load-slot [a-z1..10] to specify a drive letter and slot number for the game file.");
							return;
						}						

						char driveLetter = char.ToUpper(match.Groups[1].Value[0]);
						int slotId = int.Parse(match.Groups[2].Value);

						settings.LoadSaveGameSlot = new Tuple<char, int>(driveLetter, slotId);
						break;
					case "skip-credits": settings.ShowCredits = false; continue;
					case "skip-intro": settings.ShowIntro = false; continue;
					case "software-render": settings["software-render"] = true; continue;
                    case "seed":
                        settings.InitialSeed = short.Parse(args[++i]);
                        break;
					default: Console.WriteLine(ErrorText); return;
				}
			}

			if (settings.Free)
			{
				settings["no-sound"] = true;
			}

			using (Runtime runtime = new Runtime(settings))
			using (GameWindow window = new GameWindow(runtime, (bool)settings["software-render"]))
			{
				runtime.Log("Game started");
				window.Run();
				runtime.Log("Game stopped");
			}
		}
	}
}