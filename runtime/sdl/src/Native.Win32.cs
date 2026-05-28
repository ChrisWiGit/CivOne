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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace CivOne
{
	internal partial class Native
	{
		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F9-0000-0000-C000-000000000046")]
		private interface IShellLink
		{
			void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
			void GetIDList(out IntPtr ppidl);
			void SetIDList(IntPtr pidl);
			void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			void GetHotkey(out short pwHotkey);
			void SetHotkey(short wHotkey);
			void GetShowCmd(out int piShowCmd);
			void SetShowCmd(int iShowCmd);
			void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
			void Resolve(IntPtr hwnd, int fFlags);
			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}

		[ComImport]
		[Guid("00021401-0000-0000-C000-000000000046")]
		internal class ShellLink
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BrowseInfo 
		{
			public IntPtr hwndOwner;
			public IntPtr pidlRoot;
			public IntPtr pszDisplayName;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszTitle;
			public uint ulFlags;
			public BrowseCallbackProc lpfn;
			public IntPtr lParam;
			public int iImage;
		}

		private delegate int BrowseCallbackProc(IntPtr hwnd, int uMsg, IntPtr lParam, IntPtr lpData);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("shell32.dll")]
		private static extern IntPtr SHBrowseForFolder(ref BrowseInfo lpbi);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("shell32.dll", CharSet=CharSet.Unicode)]
		private static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("user32.dll")]
		public static extern int ShowCursor(bool bShow);

		private static string? Win32FolderBrowser(string caption)
		{
			// MAX_PATH = 260 Unicode characters = 520 bytes. The old 256-byte buffer
			// was only 128 chars and caused heap corruption for longer paths.
			IntPtr bufferAddress = IntPtr.Zero;
			IntPtr pidl = IntPtr.Zero;
			try
			{
				ShowCursor();
				bufferAddress = Marshal.AllocHGlobal(MAX_PATH * 2);

				BrowseInfo browseInfo = new()
				{
					hwndOwner = IntPtr.Zero,
					pidlRoot = IntPtr.Zero,
					lpszTitle = caption,
					ulFlags = 0x310,
					lParam = IntPtr.Zero,
					iImage = 0
				};
				pidl = SHBrowseForFolder(ref browseInfo);
				if (pidl == IntPtr.Zero || !SHGetPathFromIDList(pidl, bufferAddress))
				{
					// User pressed cancel
					return null;
				}

				return Marshal.PtrToStringUni(bufferAddress);
			}
			finally
			{
				HideCursor();
				if (bufferAddress != IntPtr.Zero)
					Marshal.FreeHGlobal(bufferAddress);
				if (pidl != IntPtr.Zero)
					Marshal.FreeCoTaskMem(pidl);
			}
		}

		private static bool Win32CreateShortcut(string name, string description, string path, string[] arguments, string workingDirectory, string icon)
		{
			IShellLink shortcut = (IShellLink)new ShellLink();
			shortcut.SetPath(path);
			shortcut.SetDescription(description);
			if (arguments.Length > 0) shortcut.SetArguments(string.Join(" ", arguments));
			shortcut.SetWorkingDirectory(workingDirectory);
			shortcut.SetIconLocation(icon, 0);

			string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{name}.lnk");
			((IPersistFile)shortcut).Save(filename, false);
			return File.Exists(filename);
		}

		// Use 32KB for file dialog buffer to support long paths (> 260 chars)
		private const int MAX_PATH_DIALOG = 32768;
		private const int MAX_PATH = 260;
		private const int WCHAR_SIZE = 2;

		private const int OFN_OVERWRITEPROMPT = 0x00000002;
		private const int OFN_EXPLORER = 0x00080000;
		private const int OFN_PATHMUSTEXIST = 0x00000800;
		private const int OFN_FILEMUSTEXIST = 0x00001000;
		private const int OFN_NOCHANGEDIR = 0x00000008;

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Unicode )]  
		internal struct OpenFilename
		{
			public int lStructSize;
			public IntPtr hwndOwner;
			public IntPtr hInstance;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrFilter;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrCustomFilter;
			public int nMaxCustFilter;
			public int nFilterIndex;
			public IntPtr lpstrFile;
			public int nMaxFile;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrFileTitle;
			public int nMaxFileTitle;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrInitialDir;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrTitle;
			public int Flags;
			public short nFileOffset;
			public short nFileExtension;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrDefExt;
			public IntPtr lCustData;
			public IntPtr lpfnHook;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpTemplateName;
			public IntPtr pvReserved;
			public int dwReserved;
			public int FlagsEx;
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetOpenFileName(ref OpenFilename ofn);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetSaveFileName(ref OpenFilename ofn);

		// CommDlgExtendedError
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern int CommDlgExtendedError();

		private static string BuildNativeFilter(string filter)
		{
			// OPENFILENAME expects MULTI_SZ: pairs separated by '\0', final '\0\0'.
			if (string.IsNullOrWhiteSpace(filter))
				return "All Files (*.*)\0*.*\0\0";

			string normalized = filter.Replace("|", "\0", StringComparison.Ordinal).TrimEnd('\0');
			return normalized + "\0\0";
		}

		internal static string Win32FileDialog(
			IntPtr ownerHwnd,
			bool save,
			string title,
			string initialFileName,
			string filter)
		{
			ShowCursor();
			IntPtr fileBuffer = IntPtr.Zero;
			int bufferChars = MAX_PATH_DIALOG;
			int bufferBytes = checked(bufferChars * WCHAR_SIZE);
			try
			{
				fileBuffer = Marshal.AllocHGlobal(bufferBytes);

				// Write initial filename as UTF-16 and always null-terminate.
				string safeInitialFileName = initialFileName ?? string.Empty;
				int maxContentChars = Math.Max(0, bufferChars - 1);
				int initialCharCount = Math.Min(safeInitialFileName.Length, maxContentChars);
				if (initialCharCount > 0)
					Marshal.Copy(safeInitialFileName.ToCharArray(), 0, fileBuffer, initialCharCount);
				Marshal.WriteInt16(fileBuffer, initialCharCount * WCHAR_SIZE, 0);

				string nativeFilter = BuildNativeFilter(filter);

				OpenFilename ofn = new()
				{
					lStructSize = Marshal.SizeOf(typeof(OpenFilename)),
					hwndOwner = ownerHwnd,
					hInstance = IntPtr.Zero,
					lpstrInitialDir = null,
					lpstrFilter = nativeFilter,
					lpstrFile = fileBuffer,
					nMaxFile = bufferChars, // Character count, not byte count.
					lpstrTitle = title,
					Flags =
						OFN_EXPLORER |
						OFN_NOCHANGEDIR |
						OFN_PATHMUSTEXIST |
						(save ? OFN_OVERWRITEPROMPT : OFN_FILEMUSTEXIST),
					// Reserved capacity is already large enough for future OFN_ALLOWMULTISELECT.
				};

				bool result = save
					? GetSaveFileName(ref ofn)
					: GetOpenFileName(ref ofn);
				// CommDlgExtendedError
				int lastError = CommDlgExtendedError();

				if (lastError != 0)
					Console.WriteLine("Get{0}FileName failed with error code {1}", save ? "Save" : "Open", lastError);

				return result ? Marshal.PtrToStringUni(fileBuffer) : null;
			}
			finally
			{
				HideCursor();
				if (fileBuffer != IntPtr.Zero)
					Marshal.FreeHGlobal(fileBuffer);
			}
		}

		private static bool Win32TryOpenUrl(string url, out string errorMessage)
		{
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				});
				errorMessage = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}
		}

		private static bool Win32TryCopyToClipboard(string text, out string errorMessage)
		{
			if (SDL.TrySetClipboardText(text))
			{
				errorMessage = string.Empty;
				return true;
			}

			errorMessage = SDL.GetSdlErrorMessage();
			return false;
		}
	}
}