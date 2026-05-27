// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CivOne
{
	internal partial class Native
	{
		private const string LIBGTK3 = "libgtk-3.so.0";
		private const string GLIB2 = "libglib-2.0.so.0";

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr gtk_file_chooser_dialog_new(IntPtr title, IntPtr parent, int action, IntPtr nil);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr gtk_file_chooser_get_filename(IntPtr raw);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_file_chooser_set_current_name(IntPtr raw, IntPtr name);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr gtk_file_filter_new();

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_file_filter_set_name(IntPtr filter, IntPtr name);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_file_filter_add_pattern(IntPtr filter, IntPtr pattern);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr gtk_dialog_add_button(IntPtr raw, IntPtr button_text, int response_id);

		[DllImport (LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_init (ref int argc, ref IntPtr argv);

		[DllImport (LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_main_iteration();

		[DllImport (LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool gtk_events_pending();

		[DllImport(LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern int gtk_dialog_run(IntPtr handle);

		[DllImport (LIBGTK3, CallingConvention = CallingConvention.Cdecl)]
		private static extern void gtk_widget_destroy (IntPtr handle);

		[DllImport (GLIB2, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr g_filename_to_utf8 (IntPtr mem, int len, IntPtr read, out IntPtr written, out IntPtr error);

		[DllImport (GLIB2, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr g_malloc(UIntPtr size);
		
		[DllImport (GLIB2, CallingConvention = CallingConvention.Cdecl)]
		private static extern void g_free (IntPtr mem);

		public static IntPtr StringToIntPtr(string input)
		{
			if (input == null) return IntPtr.Zero;

			byte[] bytes = Encoding.UTF8.GetBytes(input);
			IntPtr output = g_malloc(new UIntPtr ((ulong)bytes.Length + 1));
			Marshal.Copy (bytes, 0, output, bytes.Length);
			Marshal.WriteByte(output, bytes.Length, 0);

			return output;
		}

		public static string GetFileName(IntPtr input) 
		{
			if (input == IntPtr.Zero) return null;

			IntPtr written, error;
			IntPtr filename = g_filename_to_utf8(input, -1, IntPtr.Zero, out written, out error);

			int i = 0;
			byte[] bytes;
			while(true)
			{
				bytes = new byte[++i];
				Marshal.Copy(filename, bytes, 0, i);
				if (bytes[bytes.GetUpperBound(0)] == 0) break;
			}
			
			return Encoding.UTF8.GetString(bytes.Take(bytes.Length - 1).ToArray());
		}

		private static IntPtr AddButton(IntPtr handle, string text, int responseId)
		{
			IntPtr native_button_text = StringToIntPtr(text);
			IntPtr raw_ret = gtk_dialog_add_button(handle, native_button_text, responseId);
			g_free(native_button_text);
			return raw_ret;
		}

		private static string GtkFolderBrowser(string caption)
		{
			IntPtr title = StringToIntPtr(caption);
			IntPtr test = gtk_file_chooser_dialog_new(title, IntPtr.Zero, 2, IntPtr.Zero);
			g_free(title);

			AddButton(test, "Cancel", -6);
			AddButton(test, "OK", -5);

			string output = null;
			if (gtk_dialog_run(test) == -5)
			{
				IntPtr response = gtk_file_chooser_get_filename(test);
				string test2 = GetFileName(response);
				g_free(response);
				output = test2;
			}
			gtk_widget_destroy(test);
			while (gtk_events_pending())
				gtk_main_iteration();
			return output;
		}

		private static string GtkFileDialog(bool save, string title, string initialFileName, string filter)
		{
			// action: 0 = GTK_FILE_CHOOSER_ACTION_OPEN, 1 = GTK_FILE_CHOOSER_ACTION_SAVE
			int action = save ? 1 : 0;
			IntPtr nativeTitle = StringToIntPtr(title);
			IntPtr dialog = gtk_file_chooser_dialog_new(nativeTitle, IntPtr.Zero, action, IntPtr.Zero);
			g_free(nativeTitle);

			if (save && !string.IsNullOrEmpty(initialFileName))
			{
				IntPtr nativeName = StringToIntPtr(initialFileName);
				gtk_file_chooser_set_current_name(dialog, nativeName);
				g_free(nativeName);
			}

			// filter format: "Description (*.ext)|*.ext|All Files (*.*)|*.*"
			if (!string.IsNullOrEmpty(filter))
			{
				string[] parts = filter.Split('|');
				for (int i = 0; i + 1 < parts.Length; i += 2)
				{
					IntPtr gtkFilter = gtk_file_filter_new();
					IntPtr nativeName = StringToIntPtr(parts[i]);
					gtk_file_filter_set_name(gtkFilter, nativeName);
					g_free(nativeName);
					IntPtr nativePattern = StringToIntPtr(parts[i + 1]);
					gtk_file_filter_add_pattern(gtkFilter, nativePattern);
					g_free(nativePattern);
					gtk_file_chooser_add_filter(dialog, gtkFilter);
				}
			}

			AddButton(dialog, "Cancel", -6);
			AddButton(dialog, save ? "Save" : "Open", -5);

			string output = null;
			if (gtk_dialog_run(dialog) == -5)
			{
				IntPtr response = gtk_file_chooser_get_filename(dialog);
				output = GetFileName(response);
				g_free(response);
			}
			gtk_widget_destroy(dialog);
			while (gtk_events_pending())
				gtk_main_iteration();
			return output;
		}

		private static bool LinuxTryOpenUrl(string url, out string errorMessage)
		{
			// Try xdg-open first
			if (TryExecuteCommand("xdg-open", url, out errorMessage))
				return true;

			// Fallback: try gio open
			if (TryExecuteCommand("gio", $"open {url}", out errorMessage))
				return true;

			// Last resort: gnome-open
			if (TryExecuteCommand("gnome-open", url, out errorMessage))
				return true;

			errorMessage = "Could not open URL: xdg-open, gio, and gnome-open not available.";
			return false;
		}

		private static bool LinuxTryCopyToClipboard(string text, out string errorMessage)
		{
			// Try xclip first
			if (TryExecuteCommandWithInput("xclip", "-selection", "clipboard", text, out errorMessage))
				return true;

			// Fallback: try xsel
			if (TryExecuteCommandWithInput("xsel", "--clipboard", "--input", text, out errorMessage))
				return true;

			errorMessage = "Could not copy to clipboard: xclip and xsel not available.";
			return false;
		}

		private static bool TryExecuteCommand(string command, string argument, out string errorMessage)
		{
			try
			{
				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = command,
					Arguments = argument,
					UseShellExecute = false,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using (var process = System.Diagnostics.Process.Start(psi))
				{
					if (process == null || !process.WaitForExit(5000) || process.ExitCode != 0)
					{
						errorMessage = $"Command failed with exit code {process?.ExitCode ?? -1}";
						return false;
					}
					errorMessage = string.Empty;
					return true;
				}
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}
		}

		private static bool TryExecuteCommandWithInput(string command, string arg1, string arg2, string input, out string errorMessage)
		{
			try
			{
				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = command,
					Arguments = $"{arg1} {arg2}",
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using (var process = System.Diagnostics.Process.Start(psi))
				{
					if (process == null)
					{
						errorMessage = "Command process could not be started.";
						return false;
					}

					using (var writer = process.StandardInput)
					{
						writer.Write(input);
					}

					if (!process.WaitForExit(5000))
					{
						errorMessage = "Command execution timed out.";
						return false;
					}

					if (process.ExitCode != 0)
					{
						string standardError = process.StandardError.ReadToEnd();
						errorMessage = !string.IsNullOrWhiteSpace(standardError)
							? standardError.Trim()
							: $"Command failed with exit code {process.ExitCode}";
						return false;
					}

					errorMessage = string.Empty;
					return true;
				}
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}
		}
	}
}