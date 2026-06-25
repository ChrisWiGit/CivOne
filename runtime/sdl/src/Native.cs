// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CivOne.Enums;

namespace CivOne
{
	internal sealed partial class Native
	{
		private static IntPtr _handle = IntPtr.Zero;

		internal static Platform Platform =>
			true switch
			{
				_ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => Platform.Windows,
				_ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   => Platform.Linux,
				_ when RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     => Platform.macOS,
				_ => throw new PlatformNotSupportedException("Unsupported platform")
			};

		internal static string? FolderBrowser(string caption = "")
		{
			switch (Platform)
			{
				case Platform.Windows:
					return Win32FolderBrowser(caption);
				case Platform.Linux:
					return GtkFolderBrowser(caption);
				case Platform.macOS:
					return MacFolderBrowser(caption);
				default:
					return null;
			}
		}
		
		internal static string? FileChooser(
			bool save,
			string title,
			string initialFileName,
			string filter)
		{
			switch (Platform)
			{
				case Platform.Windows:
					return Win32FileDialog(
						SDL.GetSDLWindowHandle(_handle),
						save,
						title,
						initialFileName,
						filter
					);
				case Platform.Linux:
					return GtkFileDialog(save, title, initialFileName, filter);
				case Platform.macOS:
					return MacFileChooser(save, title, initialFileName, filter);					
				default:
					return null;
			}
		}

	internal static bool TryOpenUrl(string url, out string errorMessage)
	{
		switch (Platform)
		{
			case Platform.Windows:
				return Win32TryOpenUrl(url, out errorMessage);
			case Platform.Linux:
				return LinuxTryOpenUrl(url, out errorMessage);
			case Platform.macOS:
				return MacTryOpenUrl(url, out errorMessage);
			default:
				errorMessage = "Unsupported platform.";
				return false;
		}
	}

	internal static bool TryCopyToClipboard(string text, out string errorMessage)
	{
		switch (Platform)
		{
			case Platform.Windows:
				return Win32TryCopyToClipboard(text, out errorMessage);
			case Platform.Linux:
				return LinuxTryCopyToClipboard(text, out errorMessage);
			case Platform.macOS:
				return MacTryCopyToClipboard(text, out errorMessage);
			default:
				errorMessage = "Unsupported platform.";
				return false;
		}
	}

	internal static void ShowCursor()
	{
		switch (Platform)
		{
			case Platform.Windows:
				_ = ShowCursor(true);
				break;
		}
	}

	internal static void HideCursor()
	{
		switch (Platform)
		{
			case Platform.Windows:
				_ = ShowCursor(false);
				break;
		}
	}

	internal static bool CreateDesktopIcon(string name, string description, params string[] arguments)
		{
			switch (Platform)
			{
				case Platform.Windows:
					if (Resources.BinPath == null) return false;
#if DEBUG
					string path = "dotnet";
					arguments = [.. new [] { $@"""{Path.Combine(Resources.BinPath, "CivOne.SDL.dll")}""" }.Union(arguments)];
#else
					if (!File.Exists(Path.Combine(Resources.BinPath, "CivOne.SDL.exe"))) return false;
					string path = Path.Combine(Resources.BinPath, "CivOne.SDL.exe");
#endif

					return Win32CreateShortcut(name, description, path, arguments, Environment.CurrentDirectory, Path.Combine(Resources.BinPath, "CivOne.ico"));
				default:
					return false;
			}
		}

		internal static void Init(IntPtr handle)
		{
			_handle = handle;

			switch (Platform)
			{
				case Platform.Windows:
					break;
				case Platform.Linux:
					// Init GTK
					IntPtr argv = new IntPtr(0);
					int argc = 0;
					gtk_init(ref argc, ref argv);
					break;
			}
		}
	}
}