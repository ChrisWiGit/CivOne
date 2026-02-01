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
		private struct BROWSEINFO 
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

		[DllImport("shell32.dll")]
		private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

		[DllImport("shell32.dll", CharSet=CharSet.Unicode)]
		private static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

		[DllImport("user32.dll")]
		public static extern int ShowCursor(bool bShow);

		private static string Win32FolderBrowser(string caption)
		{
			ShowCursor();
			IntPtr bufferAddress = Marshal.AllocHGlobal(256);
			IntPtr pidl;
			BROWSEINFO browseInfo = new BROWSEINFO()
			{
				hwndOwner = IntPtr.Zero,
				pidlRoot = IntPtr.Zero,
				lpszTitle = caption,
				ulFlags = 0x310,
				lParam = IntPtr.Zero,
				iImage = 0
			};
			pidl = SHBrowseForFolder(ref browseInfo);
			if (!SHGetPathFromIDList(pidl, bufferAddress))
			{
				// User pressed cancel
				return null;
			}

			HideCursor();
			
			return Marshal.PtrToStringUni(bufferAddress);
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
			(shortcut as IPersistFile).Save(filename, false);
			return File.Exists(filename);
		}

		private const int MAX_PATH = 260;

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Unicode )]  
		internal struct OPENFILENAME
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
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrFile;
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

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetOpenFileName(ref OPENFILENAME ofn);

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetSaveFileName(ref OPENFILENAME ofn);

		// CommDlgExtendedError
		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
		private static extern int CommDlgExtendedError();

		internal static string Win32FileDialog(
			IntPtr ownerHwnd,
			bool save,
			string title,
			string initialFileName,
			string filter)
		{
			ShowCursor();

			var fileBuffer = new StringBuilder(MAX_PATH);
			if (!string.IsNullOrEmpty(initialFileName))
				fileBuffer.Append(initialFileName);
			fileBuffer.Append('\0');

			// Win32-Filterformat:
			// "Textdateien (*.txt)\0*.txt\0Alle Dateien (*.*)\0*.*\0"
			filter = filter.Replace("|", "\0");
			if (!filter.EndsWith("\0\0"))
				filter += "\0\0";

			OPENFILENAME ofn = new OPENFILENAME
			{
				lStructSize = Marshal.SizeOf(typeof(OPENFILENAME)),
				hwndOwner = ownerHwnd,
				hInstance = IntPtr.Zero,
				lpstrInitialDir = null,
				lpstrFilter = filter,
				lpstrFile = fileBuffer.ToString(),
				nMaxFile = MAX_PATH,
				lpstrTitle = title,
				Flags =
					0x00000008 | // OFN_EXPLORER
					0x00001000 | // OFN_PATHMUSTEXIST
					(save ? 0x00000002 : 0x00000800), // SAVE: OVERWRITEPROMPT, OPEN: FILEMUSTEXIST
			};

			bool result = save
				? GetSaveFileName(ref ofn)
				: GetOpenFileName(ref ofn);
			// CommDlgExtendedError
			int lastError = CommDlgExtendedError();

			if (lastError != 0)
				Console.WriteLine("Get{0}FileName failed with error code {1}", save ? "Save" : "Open", lastError);

			HideCursor();

			return result ? ofn.lpstrFile.ToString() : null;
		}
	}
}