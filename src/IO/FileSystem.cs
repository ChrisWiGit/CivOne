// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Screens.Dialogs;

namespace CivOne.IO
{
	public static class FileSystem
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private static string[] DATA_FILES = new string[] { "FONTS.CV", "ADSCREEN.PIC", "ARCH.PIC", "BACK0A.PIC", "BACK0M.PIC", "BACK1A.PIC", "BACK1M.PIC", "BACK2A.PIC", "BACK2M.PIC", "BACK3A.PIC", "BIRTH0.PIC", "BIRTH1.PIC", "BIRTH2.PIC", "BIRTH3.PIC", "BIRTH4.PIC", "BIRTH5.PIC", "BIRTH6.PIC", "BIRTH7.PIC", "BIRTH8.PIC", "CASTLE0.PIC", "CASTLE1.PIC", "CASTLE2.PIC", "CASTLE3.PIC", "CASTLE4.PIC", "CBACK.PIC", "CBACKS1.PIC", "CBACKS2.PIC", "CBACKS3.PIC", "CBRUSH0.PIC", "CBRUSH1.PIC", "CBRUSH2.PIC", "CBRUSH3.PIC", "CBRUSH4.PIC", "CBRUSH5.PIC", "CITYPIX1.PIC", "CITYPIX2.PIC", "CITYPIX3.PIC", "CUSTOM.PIC", "DIFFS.PIC", "DISCOVR1.PIC", "DISCOVR2.PIC", "DOCKER.PIC", "GOVT0A.PIC", "GOVT0M.PIC", "GOVT1A.PIC", "GOVT1M.PIC", "GOVT2A.PIC", "GOVT2M.PIC", "GOVT3A.PIC", "HILL.PIC", "ICONPG1.PIC", "ICONPG2.PIC", "ICONPG3.PIC", "ICONPG4.PIC", "ICONPG5.PIC", "ICONPG6.PIC", "ICONPG7.PIC", "ICONPG8.PIC", "ICONPGA.PIC", "ICONPGB.PIC", "ICONPGC.PIC", "ICONPGD.PIC", "ICONPGE.PIC", "ICONPGT1.PIC", "ICONPGT2.PIC", "INVADER2.PIC", "INVADER3.PIC", "INVADERS.PIC", "KING00.PIC", "KING01.PIC", "KING02.PIC", "KING03.PIC", "KING04.PIC", "KING05.PIC", "KING06.PIC", "KING07.PIC", "KING08.PIC", "KING09.PIC", "KING10.PIC", "KING11.PIC", "KING12.PIC", "KING13.PIC", "KINK00.PIC", "KINK03.PIC", "LOGO.PIC", "LOVE1.PIC", "LOVE2.PIC", "MAP.PIC", "NUKE1.PIC", "PLANET1.PIC", "PLANET2.PIC", "POP.PIC", "RIOT.PIC", "RIOT2.PIC", "SAD.PIC", "SETTLERS.PIC", "SLAG2.PIC", "SLAM1.PIC", "SLAM2.PIC", "SP257.PIC", "SP299.PIC", "SPACEST.PIC", "SPRITES.PIC", "TER257.PIC", "TORCH.PIC", "WONDERS.PIC", "WONDERS2.PIC", "BACK0A.PAL", "BACK0M.PAL", "BACK1A.PAL", "BACK1M.PAL", "BACK2A.PAL", "BACK2M.PAL", "BACK3A.PAL", "BIRTH0.PAL", "BIRTH1.PAL", "BIRTH2.PAL", "BIRTH3.PAL", "BIRTH4.PAL", "BIRTH5.PAL", "BIRTH6.PAL", "BIRTH7.PAL", "BIRTH8.PAL", "DISCOVR1.PAL", "DISCOVR2.PAL", "HILL.PAL", "ICONPG1.PAL", "ICONPGA.PAL", "KING00.PAL", "KING01.PAL", "KING02.PAL", "KING03.PAL", "KING04.PAL", "KING05.PAL", "KING06.PAL", "KING07.PAL", "KING08.PAL", "KING09.PAL", "KING10.PAL", "KING11.PAL", "KING12.PAL", "KING13.PAL", "SLAM1.PAL", "SP256.PAL", "SP257.PAL", "BLURB0.TXT", "BLURB1.TXT", "BLURB2.TXT", "BLURB3.TXT", "BLURB4.TXT", "CREDITS.TXT", "ERROR.TXT", "HELP.TXT", "KING.TXT", "PRODUCE.TXT", "STORY.TXT", "CIV.EXE" };
		private static string[] SOUND_FILES = new string[] { "AIRNUKE.WAV", "ALEX.WAV", "CANNON.WAV", "CEAS.WAV", "ELIZ.WAV", "FRED.WAV", "GAND.WAV", "GENG.WAV", "HAMA.WAV", "LINC.WAV", "LOSE2.WAV", "MAO.WAV", "MONT.WAV", "NAPO.WAV", "OPENING.WAV", "RAMS.WAV", "S_BEEP.WAV", "S_LAND.WAV", "S_NUKE.WAV", "SHAK.WAV", "STAL.WAV", "THEY_DIE.WAV", "WE_DIE.WAV", "WINTUNE.WAV" };

		internal static string[] MouseCursorFiles
		{
			get
			{
				return new string[] { "SP257.PIC", "SP257.PAL" };
			}
		}

		public static bool DataFilesExist(params string[] files)
		{
			Log("Checking data files in {0}...", Settings.Instance.DataDirectory);
			if (files.Length == 0) files = DATA_FILES;
			if (!Directory.Exists(Settings.Instance.DataDirectory))
			{
				Log("Target data directory does not exist: {0}", Settings.Instance.DataDirectory);
				return false;
			}
			HashSet<string> existingFiles = EnumerateFileNames(Settings.Instance.DataDirectory);
			foreach (string filename in files)
			{
				if (existingFiles.Contains(filename)) continue;

				Log("Target resource file does not exist: {0}", filename);
				return false;
			}
			Log("Done, all files exist");
			return true;
		}
		
		public static bool CopyDataFiles(string folder)
		{
			Log("Copying data files to {0}...", Settings.Instance.DataDirectory);
			if (!CopyFiles(folder, Settings.Instance.DataDirectory, DATA_FILES, out string missingFile))
			{
				Log("- File not found: {0}", missingFile);
				return false;
			}
			Log("- Done, all copied");
			return true;
		}

		public static bool SoundFilesExist(params string[] files)
		{
			Log("Checking sound files...");
			if (files.Length == 0) files = SOUND_FILES;
			if (!Directory.Exists(Settings.Instance.SoundsDirectory))
			{
				Log("Target sound directory does not exist: {0}", Settings.Instance.SoundsDirectory);
				return false;
			}
			HashSet<string> existingFiles = EnumerateFileNames(Settings.Instance.SoundsDirectory);
			foreach (string filename in files)
			{
				if (existingFiles.Contains(filename)) continue;

				Log("- File not found: {0}", filename);
				return false;
			}
			Log("- Done, all files exist");
			return true;
		}

		public static bool CopySoundFiles(string folder)
		{
			Log("Copying sound files...");
			if (!CopyFiles(folder, Settings.Instance.SoundsDirectory, SOUND_FILES, out string missingFile))
			{
				Log("- File not found: {0}", missingFile);
				return false;
			}
			Log("- Done, all copied");
			return true;
		}

		internal static bool CopyFiles(string sourceDirectory, string targetDirectory, IReadOnlyList<string> fileNames, out string missingFile)
		{
			Directory.CreateDirectory(targetDirectory);
			foreach (string filename in fileNames)
			{
				string targetPath = Path.Combine(targetDirectory, filename);

				// Linux: a previous install may have left the file under a different
				// casing. Normalize it to the canonical name so subsequent lookups via
				// the exact path succeed.
				string existingTarget = FindFileIgnoreCase(targetDirectory, filename);
				if (existingTarget != null)
				{
					if (!string.Equals(existingTarget, targetPath, StringComparison.Ordinal))
					{
						Log("- Normalizing target file casing: {0} -> {1}", Path.GetFileName(existingTarget), filename);
						MoveFileToCanonicalCasing(existingTarget, targetPath);
					}
					continue;
				}

				string sourcePath = FindFileIgnoreCase(sourceDirectory, filename);
				if (sourcePath != null)
				{
					File.Copy(sourcePath, targetPath);
					continue;
				}

				missingFile = filename;
				return false;
			}

			missingFile = null;
			return true;
		}

		/// <summary>
		/// Moves <paramref name="sourcePath"/> to <paramref name="targetPath"/> and enforces
		/// the exact target casing.
		/// </summary>
		/// <remarks>
		/// A direct case-only rename can fail on case-insensitive file systems because source and
		/// destination may be treated as the same path.
		/// To keep behavior consistent across platforms, this method uses a temporary intermediate
		/// name when only casing differs.
		/// </remarks>
		/// <param name="sourcePath">Current path of the file.</param>
		/// <param name="targetPath">Destination path with canonical casing.</param>
		private static void MoveFileToCanonicalCasing(string sourcePath, string targetPath)
		{
			if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(sourcePath, targetPath, StringComparison.Ordinal))
			{
				// Refactoring note: a direct case-only rename can fail on case-insensitive
				// file systems because source and destination are treated as the same path.
				// Rename via a temporary file first to make the casing normalization reliable.
				string directory = Path.GetDirectoryName(targetPath) ?? string.Empty;
				string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
				File.Move(sourcePath, temporaryPath);
				File.Move(temporaryPath, targetPath);
				return;
			}

			File.Move(sourcePath, targetPath);
		}

		/// <summary>
		/// Returns the on-disk file path that matches <paramref name="filename"/> in the given folder,
		/// using case-insensitive name comparison. Returns <see langword="null"/> when no match exists.
		/// </summary>
		/// <remarks>
		/// Required for Linux/macOS where the file system is case-sensitive but the original DOS game
		/// data uses UPPERCASE names that users may have renamed.
		/// </remarks>
		internal static string FindFileIgnoreCase(string folder, string filename)
		{
			if (!Directory.Exists(folder))
			{
				return null;
			}

			return Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
				.FirstOrDefault(path => string.Equals(Path.GetFileName(path), filename, StringComparison.OrdinalIgnoreCase));
		}

		private static HashSet<string> EnumerateFileNames(string folder)
		{
			HashSet<string> output = new(StringComparer.OrdinalIgnoreCase);
			if (!Directory.Exists(folder))
			{
				return output;
			}

			foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
			{
				output.Add(Path.GetFileName(path));
			}
			return output;
		}

		public static bool CopyPlugins(string folder)
		{
			Queue<OverwritePlugin> dialogs = new Queue<OverwritePlugin>();

			Log("Copying plugins...");
			foreach (string filepath in Directory.GetFiles(folder))
			{
				if (!Plugin.Validate(filepath))
				{
					Log($"- Invalid plugin file: {filepath}");
					continue;
				}

				string filename = Path.GetFileName(filepath);
				string destinationFile = Path.Combine(Settings.Instance.PluginsDirectory, Path.GetFileName(filename));
				if (File.Exists(destinationFile))
				{
					Log($"- Plugin already exists: {filepath}");
					dialogs.Enqueue((OverwritePlugin)OverwritePluginDialogFactory.CreateDialog(filepath, destinationFile));
					continue;
				}

				Log($"- Plugin copied: {filepath}");
				File.Copy(filepath, destinationFile);
				Reflect.LoadPlugin(destinationFile);
			}

			Action nextDialog = null;
			nextDialog = () =>
			{
				if (dialogs.Count == 0) return;
				OverwritePlugin dialog = dialogs.Dequeue();
				dialog.Closed += (s, a) => nextDialog();
				Common.AddScreen(dialog);
			};
			nextDialog();

			return true;
		}
	} 
}