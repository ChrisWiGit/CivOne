// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CivOne
{
	internal partial class Native
	{
		private static string? MacFolderBrowser(string caption)
		{
			string? dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (dirName == null) return null;

			string scriptPath = Path.Combine(dirName, "OpenFolder.sh");
			using (StreamWriter sw = new(scriptPath, false))
			{
				sw.WriteLine("!#/bin/bash");
				sw.WriteLine($@"osascript -e 'set getPath to choose folder with prompt ""{caption}""' -e 'set output to POSIX path of getPath'");
				sw.Flush();
			}

			Process.Start("chmod", $@"+x ""{scriptPath}""");

			Process process = new Process();
			process.StartInfo = new ProcessStartInfo()
			{
				FileName = "/bin/sh",
				Arguments = scriptPath,
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			
			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim(['\n', '"']);
			process.WaitForExit();

			if (output.Length == 0 || !Directory.Exists(output)) return null;

			return output;
		}

		private static string? MacFileChooser(bool save, string title, string initialFileName, string filter)
		{
			string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (path == null) return null;

			string scriptPath = Path.Combine(path, "FileChooser.sh");

			string appleScript = save
				? BuildSaveScript(title, initialFileName)
				: BuildOpenScript(title);

			using (StreamWriter sw = new StreamWriter(scriptPath, false))
			{
				sw.WriteLine("#!/bin/bash");
				sw.WriteLine(appleScript);
				sw.Flush();
			}

			Process.Start("chmod", $@"+x ""{scriptPath}""");

			Process process = new Process();
			process.StartInfo = new ProcessStartInfo()
			{
				FileName = "/bin/sh",
				Arguments = scriptPath,
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim(['\n', '"']);
			process.WaitForExit();

			if (output.Length == 0) return null;
			if (!save && !File.Exists(output)) return null;

			return output;
		}

		private static string BuildSaveScript(string title, string initialFileName)
		{
			string defaultName = string.IsNullOrEmpty(initialFileName)
				? ""
				: $" default name \"{initialFileName}\"";
			return $"osascript -e 'set result to choose file name with prompt \"{title}\"{defaultName}' -e 'set output to POSIX path of result' -e 'return output'";
		}

		private static string BuildOpenScript(string title) =>
			$"osascript -e 'set result to choose file with prompt \"{title}\"' -e 'set output to POSIX path of result' -e 'return output'";
	private static bool MacTryOpenUrl(string url, out string errorMessage)
	{
		try
		{
			Process.Start("open", url);
			errorMessage = string.Empty;
			return true;
		}
		catch (System.Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool MacTryCopyToClipboard(string text, out string errorMessage)
	{
		try
		{
			var process = new Process();
			process.StartInfo = new ProcessStartInfo()
			{
				FileName = "pbcopy",
				UseShellExecute = false,
				RedirectStandardInput = true
			};
			process.Start();
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			process.WaitForExit();
			errorMessage = string.Empty;
			return true;
		}
		catch (System.Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}	}
}